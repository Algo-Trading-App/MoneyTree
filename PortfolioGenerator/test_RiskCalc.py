import unittest
import Library.RiskCalc as rc
import logging as log

class MyTestCase(unittest.TestCase):
    def test_VAR(self):
        rand_returns_distribution = rc.np.random.normal(0, .8, 100)
        df_valid = rc.pd.Series(rand_returns_distribution)
        horizion_valid = 1
        df_invalid = rc.pd.Series([],dtype=float)
        horizion_invalid = rc.np.random.uniform(-1000,0)

        # var value needs to be in returns series so that
        # there is something to cutoff the returns distribution
        result = rc.VAR(df_valid, horizion_valid)
        self.assertEqual(True, result in set(df_valid))

        # edge cases of invalid parameters in function
        # empty series or invalid horizon
        function_state = True
        try:
            result = rc.VAR(df_invalid, horizion_valid)
        except Exception:
           function_state = False
        self.assertEqual(False, function_state)

        function_state = True
        try:
            result = rc.VAR(df_valid, horizion_invalid)
        except Exception:
            function_state = False
        self.assertEqual(False, function_state)

    def test_Entropy(self):
        rand_returns_distribution = rc.np.random.normal(0, .8, 100)
        df_valid = rc.pd.Series(rand_returns_distribution)
        cutoff_valid = rc.np.random.choice(rand_returns_distribution,1)
        df_invalid = rc.pd.Series([], dtype=float)

        # entropy value needs to be < 0 by definition of entropy
        result = rc.Entropy(df_valid, cutoff_valid)
        self.assertEqual(True,result < 0 )

        # edge cases of invalid parameters in function
        # empty series
        function_state = True
        try:
            result = rc.Entropy(df_invalid, cutoff_valid)
        except Exception:
            function_state = False
        self.assertEqual(False, function_state)

    def test_PosReturns(self):
        rand_returns_distribution = rc.np.random.normal(0, .8, 100)
        df_valid = rc.pd.Series(rand_returns_distribution)
        df_invalid = rc.pd.Series([], dtype=float)

        # PosReturns value needs to be bewteen 0 and 1 since this is the
        # probability of positive returns
        result = rc.PosReturns(df_valid)
        self.assertEqual(True,result >= 0 and result <= 1 )

        # edge cases of invalid parameters in function
        # empty series
        function_state = True
        try:
            result = rc.PosReturns(df_invalid)
        except Exception:
            function_state = False
        self.assertEqual(False, function_state)


if __name__ == '__main__':
    unittest.main()
