#!/usr/bin/env python
import pika
import json
import zipfile
import quandl
import os
import pandas
from zipfile import ZipFile

# Quandl API Key
API_KEY = "b5wYcieS5ZYxGvJNN7yW"


class RabbitMQBroker():
	def __init__(self, apiKey):
		self.APIKey = apiKey


	def callback(self, ch, method, properties, body):
		try:
			equityCall = body.decode("utf8").replace("\'", "\"")
			equityCall = json.loads(equityCall)

			print(equityCall)

		except:
			print("RECIEVE: Incorrect RabbitMQ message format")


	def recieve(self):
		connection = pika.BlockingConnection(
		pika.ConnectionParameters(host="localhost"))
		channel = connection.channel()

		channel.queue_declare(queue="rabbitBroker")
		channel.basic_consume(queue="rabbitBroker", on_message_callback=self.callback, auto_ack=True)

		print("RECIEVER: [*] Waiting for messages. To exit press CTRL+C")
		channel.start_consuming()


	def send(self, message, queue, routingKey):
	    connection = pika.BlockingConnection(
	    pika.ConnectionParameters(host="localhost"))
	    channel = connection.channel()

	    channel.queue_declare(queue=queue, auto_delete=True)

	    channel.basic_publish(exchange="", routing_key=routingKey, body=str(message))
	    print(" [x] Sent message to queue:", queue)

	    connection.close()


def main():
	# Create porfolio generator instance
	generator = PortfolioGenerator(API_KEY)

	# Opens example message for RabbitMQMessage
	with open("message/PortfolioGeneratorQuery.json", "r") as message:
		message = json.loads(message.read())
		generator.send(message, "backtest", "backtest")
		generator.send(message, "backtestTrigger", "backtestTrigger")

	generator.recieve()



if __name__ == "__main__":
    main()
