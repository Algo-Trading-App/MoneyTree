

## Module Overview ##

These are the modules that have been developed as the endpoints for Lean's interactivity:

 - **Data Fetcher Emulator**
   > Connect and download data required for the algorithmic trading engine. For backtesting this sources files from the disk, for live trading it connects to a stream and generates the data objects.

 - **Portfolio Generator Emulator**
   > Handle all messages from the algorithmic trading engine to the portfolio generator module. Decide what should be sent, and where the messages should go. The results of the processes are output to the console.

## Environment Setup ##
1. To be able to run the module emulators, you must first have the python package `pika` installed and working on your system. To get started with this, follow the  [setup instructions for RabbitMQ]([https://www.rabbitmq.com/download.html]).
2. Once pika has been installed, run `rabbitmq-server` from your command line to get the instance of RabbitMQ running.
3. After this has been installed, open `QuantConnect.Lean.sln` in visual studio.
4. Once you have opened the project and have loaded the Lean solution, select *Restore NuGet Packages* from the *Project* dropdown in visual studio.
5. That should be it and you should be ready to go :)
## Running Instructions(In order) ##
**Data Fetcher Emulator**
1. To run the data fetcher module, you must run `python main.py` from the directory `/Modules` with the `-d` flag.
2. This emulator simulates a RabbitMQ call to the DataFetcher using a query generated from the JSON in `/Modules/DataFetcher` called `/Modules/DataFetcher`
3. If working correctly the data from the JSON should be loading into the required directory for use in the Lean Instance.

**Portfolio Generator Emulator**
5. To run the Portfolio generator module, you must first run `python main.py` from the directory `/Modules` with the `-g` flag.
6. This makes a call to the DataFetcher using a query generated from the JSON in `ModuleEmulator/PortfolioGenEmulator` called `ModuleEmulator/PortfolioGeneratorQuery.json`
7. If working correctly the data from the JSON should be loading into the required directory for use in the Lean Instance.

**Lean Backtest**
9. Once the program has finished compiling and has started running, make sure the data fetcher module is running.
10. To run the Lean Backtest, select *run* from the top bar of visual studio
11. The results of the backtest can then be viewed from the console of the portfolio generator module or the output window for visual studio.
