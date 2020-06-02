from dataclasses import dataclass
from typing import List

@dataclass()
class Asset():
    ticker: str #asset ticker symbol
    name: str #full name
    asset_type: str # type of asset stocks, bonds, crypto ..
    price_history_file: str #absolute path location of CSV file with historical data of stock
                            #might not be the best way, but im open to ideas for
                            # how to define a member that encapsulates the stocks prices
    last_price: float # latest price of asset, used for generating portfolio to ensure correct allocation
@dataclass
class Universe():
    count: int
    universe_set: List[Asset]

@dataclass()
class Portfolio():
    user_id: int    # unique identifier for user to know who's portfolio it is
    buying_power: float # Money available for this user that has not being invested yet
    # value_at_risk: float # a risk calculation that I will later implement
    # cond_value_at_risk: float # a risk calculation that I will later implement
    assets: List[Asset] # a list of stocks in this portfolio
    asset_alloc: dict
