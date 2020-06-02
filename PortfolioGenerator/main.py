#imports
from Library.RabbitMQConsumer import *
from Library.MonteCarloSim import *
from Library.WalkForward import backtest, backtest_bollinger
from Library.GeneratePortfolio import *

# ### for live testing ####
# # # self trigger a request to Portfolio Generator
#rabbitmq = RabbitMqProducer('PortfGen', "localhost", "PortfGen","")
#pm_request = PGRequest(PGRequestType.Generate)
#rabbitmq.publish(pm_request.to_json())

# # # Portfolio generator waiting for requests to handel
server = RabbitMqConsumer('PortfGen', "localhost")
server.startserver()