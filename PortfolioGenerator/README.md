# PortfolioGenerator
 Steps for running module:
 1. download or clone code.
 2. make sure you are using python 3 and install dependencies
 3. run main.py using command <b>python3 main.py</b>
 
Following these steps should set the Portfolio generator to listen for generate portfolio request.
You should see a console message saying <code> Server started waiting for Messages </code>. 
 
 <br> To send a request message to the portfolio generator use queue <b>PortfGen</b> and using <b>localhost</b> port <b>5672</b>. 
  
 <br>Once a request is sent by another module and recived by portfolio generator
  you should see the following console message <code> message recived to start generating </code>
  you will also either get a message saying <code> generating</code>
  or <code>Already generating</code> depending on whether the portfolio generator is already generating
  or just started generating. 
  
  <br> A <code>sent</code> console message indicates that the portfolio generator finished generating a portfolio
  and has sent it to the database module for insertion. This usually take a min or two. this indicates the end of the request. 
  
  <H2>Notes</H2>
 1. Currently the rabbitMQ consumer for this module is not a RPC Class, 
 so if your request message is sent expecting a response from our module it will not get one
  (i.e using RPC messaging to send request). However, I have not tested this yet so it is worth a try anyways.
 2. As a bonus, if you have the Database module running when you send a request to generate
 then you should be able to see an interaction between database module and portfolio 
 generator. This will happen after you see the <code>Sent </code> message from the portfolio generator.
 3. This module's only current functionality during a live demo is to wait for request to generate a portfolio, 
 generate a portfolio, send it to the database and then wait again for another request to generate a portfolio. 
 
 <h2>Library Files</h2>
 The following are descriptions of each file located in the library. 
 <h3>DataClass.py</h3>
 The purpose of this file is to define the data types used internally in the Portfolio Generator and to define
 the data classes needed for messaging (i.e UDM classes for Database messaging). 
<h3>FilterUniverse.py</h3>
This file holds all the filtering functions that can be used to
filter the universe down to a subset of assets. Currently, there are only two filtering techniques
defined, ADX/AROON and filtering by ranking returns. 
<h3>ReadUniverse.py</h3>
This file maintains the locally stored asset data files and defines the
asset universe. Quandl is the data fetcher used to maintain/update asset data. 
<h3>universe.csv</h3>
The purpose of this file is to list all the assets in the universe which is later read by ReadUniverse.py
The current list of assets in this file represent the SP 500.
<h3>GeneratePortfolio.py</h3>
This file contains all the functions that generate the portfolios using a genetic algorithm.
Additionally, this file contains functions for converting a portfolio to a UDM class in order to send the portfolio
over to the Database.
 <h3>MonteCarloSim.py</h3>
 This file contains the functions capable of generating Monte Carlo simulations for a set of assets. 
 Geometric Brownian Motion is currently the only type of Monte Carlo simulation available in this file. 
 <h3>RabbitMQConsumer.py</h3>
This file defines the class for the message consumer. This is the class used by the module 
to handle messages sent to the portfolio generator.
  <h3>RabbitMQProducer.py</h3>
This file defines the class for the message producer. This is the class used by the module 
to send messages to other modules. This is currently not in use.
<h3>RPC.py</h3>
This file defines a RPC class for the message producer. This is the class used by the module 
to send messages to other modules who are also using RPC classes for messaging. 
<h3>RiskCalc.py</h3>
This file contains all the main risk calculations like , VAR , Entropy, and bollengier band for portfolios. 
<h3>WalkForward.py</h3>
This file contains backtest functions with the purpose of testing the performance of the gentic algorithm.
currently only <code> backtest_bollinger() </code> is the only working backtest using bollenger bands for the risk
management strategy.  
  