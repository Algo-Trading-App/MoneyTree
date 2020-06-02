import pika

class RabbitMqProducer():
    def __init__(self, queue, host, routing_key, exchange):
        self.host = host
        self.queue = queue
        self.routing_key = routing_key
        self.exchange = exchange
        self._connection = pika.BlockingConnection(pika.ConnectionParameters(host=self.host))
        self._channel = self._connection.channel()
        self._channel.queue_declare(queue=self.queue)

    def publish(self, msg):
        self._channel.basic_publish(exchange=self.exchange,
                      routing_key=self.routing_key,
                      body=msg)
        print("Published Message: {}".format(msg))
        self._connection.close()