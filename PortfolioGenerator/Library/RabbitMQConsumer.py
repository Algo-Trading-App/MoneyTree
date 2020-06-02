from Library.GeneratePortfolio import *

class RabbitMqConsumer():
    def __init__(self, queue, host):
        self.host = host
        self.queue = queue
        self._connection = pika.BlockingConnection(pika.ConnectionParameters(host=self.host))
        self._channel = self._connection.channel()
        self._tem = self._channel.queue_declare(queue=self.queue)
        self.generating_thread = GeneratingThread()

    def callback(self,ch,method, properties, body):
        print(body)
        msg_decoded = body.decode("utf-8")
        msg = PGRequest.from_json(msg_decoded)
        print(msg, "gets here!")
        if msg.PGMessageType == PGRequestType.Generate.value:
            print("message recived to start generating")
            if self.generating_thread.is_alive():
                print("Already generating")
            else:
                # create and run thread
                self.generating_thread = GeneratingThread()
                self.generating_thread.start()
                print("generating")
        else:
            print("invalid request type: {}".format(PGRequestType.Generate.value))

    def startserver(self):
        self._channel.basic_consume(
            queue=self.queue,
            on_message_callback=self.callback,
            auto_ack=True)
        print("Server started waiting for Messages ")
        self._channel.start_consuming()
