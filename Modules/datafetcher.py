#!/usr/bin/env python
import pika
import json
import zipfile
import quandl
import os
import pandas
from zipfile import ZipFile

# Quandl API Key
API_KEY = "PgJuoJUUrmZVu75mRUD2"

# Market Security type dictionary
MARKET_DICT = {
	"forex":"BOE/",
	"equities":"WIKI/",
	"futures":"CFTC/",
	"options":"OPT/",
	"cryptos":"BCHARTS/",
}

def main():
	# Opens example message for RabbitMQMessage
	with open("message/equityTestr.json", "r") as message:
		message = json.loads(message.read())
		send(message, "dataFetcher", "dataFetcher")

	fetcher = DataFetcher()
	fetcher.recieve()

class DataFetcher():
	def process(self, query):
		try:
			# Gets each equity for each timeframe
			for timeFrame in query["timeFrames"]:

				# haewon code: if the security is equity or forex or option or future or crpyto
				print(timeFrame["securityType"])
				# Gets each equity details from the timeframe
				for equity in timeFrame["securities"]:
					print(self.getData(timeFrame, equity, timeFrame["securityType"]))
					self.writeData(timeFrame, equity, timeFrame["securityType"])

		except:
			print("RECIEVE: Incorrect RabbitMQ message format")

	# To get Tickers by RabbitMQMessage
	def callback(self, ch, method, properties, body):
		equityCall = body.decode("utf8").replace("\'", "\"")
		equityCall = json.loads(equityCall)

		self.process(equityCall)

	# ticker = company name
	# df = stock market data from Quandl
	def getData(self, equityCall, ticker, securityType):
		# Makes Quandl API call for ticker as provided by RabbitMQMessage
		df = quandl.get(
		            MARKET_DICT[securityType] + ticker,
		            start_date=equityCall["startTime"],
		            end_date=equityCall["endTime"],
		            api_key=API_KEY)

		# Multiply values by 10000 to fit Lean format
		for header in df.columns[0:4].tolist():
		        df[header] = df[header].apply(lambda x: int(x * 10000))
		# make them int
		df["Volume"] = df["Volume"].apply(lambda x: int(x))
		# change time format to fit Lean format
		df.index = pandas.to_datetime(df.index,
		        format='%m/%d/%Y').strftime('%Y%m%d 00:00')

		# Drop unused columns from dataframe
		df = df.drop(["Ex-Dividend",
		                "Split Ratio",
		                "Adj. Open",
		                "Adj. High",
		                "Adj. Low",
		                "Adj. Close",
		                "Adj. Volume"],
		                axis=1)

		return df


	# save data into data directory for backtest
	def writeData(self, equityCall, ticker, securityType):
		# If path for equity does not exist create one
		outname = ticker.lower() + ".csv"
		zipname = ticker.lower() + ".zip"

		# Case for equity security type
		if (securityType == "equities"):
			outdir = "Algo-Trader-Lean/Data/equity/usa/"+equityCall["resolution"]+"/"
			if not os.path.exists(outdir):
			    os.makedirs(outdir)

		# # Case for forex security type
		# elif (securityType == "forex"):
		# 	outdir = "../Data/forex/fxcm/"+equityCall["resolution"]+"/"
		# 	if not os.path.exists(outdir):
		# 	    os.makedirs(outdir)
		#
		# # Case for equity security type
		# elif (securityType == "equities"):
		# 	outdir = "../Data/equity/usa/"+equityCall["resolution"]+"/"
		# 	if not os.path.exists(outdir):
		# 	    os.makedirs(outdir)


		# Full path to equity csvzip
		fullname = os.path.join(outdir, outname)
		zipname = os.path.join(outdir, zipname)

		df = self.getData(equityCall, ticker, securityType)
		print(df)

		# Write csvzip to path
		df.to_csv(fullname, header=False)
		ZipFile(zipname, mode="w").write(fullname, os.path.basename(fullname))
		os.remove(fullname)


	def recieve(self):
	    connection = pika.BlockingConnection(
	    pika.ConnectionParameters(host="localhost"))
	    channel = connection.channel()

	    channel.queue_declare(queue="dataFetcher")

	    channel.basic_consume(queue="dataFetcher", on_message_callback=self.callback, auto_ack=True)

	    print("RECIEVER: [*] Waiting for messages. To exit press CTRL+C")
	    channel.start_consuming()


	def send(self, message, queue, routingKey):
	    connection = pika.BlockingConnection(
	    pika.ConnectionParameters(host="localhost"))
	    channel = connection.channel()

	    channel.queue_declare(queue=queue)

	    channel.basic_publish(exchange="", routing_key=routingKey, body=str(message))
	    print(" [x] Sent message")

	    connection.close()

if __name__ == "__main__":
    main()
