from platypus import NSGAII, Problem, Real
from Library.DataClass import *
from Library.RiskCalc import *

def calc_total_value(universe: Universe, weights: [float]):
    total = 0
    for i in range(universe.count):
        total += universe.universe_set[i].last_price * weights[i]

    return total

class OptPortfolio(Problem):

    def __init__(self, universe: Universe, buying_power: float):
        super(OptPortfolio, self).__init__(universe.count,1,1)
        self.universe = universe
        self.universe_historical_data = gen_universe_hist_data(universe.universe_set, "Adj. Close")
        self.buying_power = buying_power
        self.types[:] = Real(0,1)
        self.constraints[:] = "<=0"
        self.directions[:] = Problem.MAXIMIZE

    def evaluate(self, solution):
        solution.objectives[:] = gen_fitness_value(solution.variables, self.universe_historical_data)
        solution.constraints[0] = sum(solution.variables) - 1
        #solution.constraints[1] = calc_total_value(self.universe, solution.variables) - self.buying_power        

def generate_portfolio(universe: Universe, buying_power: float, user_id: int):
    algorithm = NSGAII(OptPortfolio(universe,buying_power))
    algorithm.run(1000)

    feasible_solutions = [s for s in algorithm.result if s.feasible]

    sol = feasible_solutions[0]

    alloc = {}
    for i in range(universe.count):
        alloc[universe.universe_set[i].ticker] = sol.variables[i]

    print(alloc)

    portf = Portfolio(user_id=user_id,buying_power=buying_power*(1-sum(sol.variables)),assets=universe.universe_set,asset_alloc=alloc)

    return portf
