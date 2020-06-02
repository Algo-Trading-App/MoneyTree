import json
import sys

from datafetcher import DataFetcher
from rabbitmqbroker import RabbitMQBroker 

# Quandl API Key
API_KEY = "b5wYcieS5ZYxGvJNN7yW"

def main():
    # Load data from query JSON
    with open("message/PortfolioGeneratorQuery.json", "r") as jsonQuery:
        message = jsonQuery
        message = json.loads(message.read())


    if ("-d" in sys.argv):
        fetcher = DataFetcher()
        fetcher.process(message)

    if ("-b" in sys.argv):
        broker = RabbitMQBroker(API_KEY)
        broker.send(message, "backtest", "backtest")
        broker.send(message, "backtestTrigger", "backtestTrigger")
        broker.send(message, "backtest", "backtest")
        broker.send(message, "backtestTrigger", "backtestTrigger")
        broker.recieve()


    if ("-g" in sys.argv):
        broker = RabbitMQBroker(API_KEY)
        broker.send(0, "PortfGen", "PortfGen")
        broker.recieve()

#    if ("-m" in sys.argv):



if __name__ == "__main__":
    main()
