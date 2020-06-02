import React from 'react';
import { makeStyles } from '@material-ui/core/styles';
import TextField from '@material-ui/core/TextField';

const useStyles = makeStyles((theme) => ({
    root: {
        '& > *': {
            margin: theme.spacing(1),
            width: '25ch',
        },
    },
}));


export function PortfolioStatWrapper({ PortfolioLength, CurrentValue, ProfitLossD,  ProfitLossP, Drawdown, Trades}) {
    const classes = useStyles();

    return (
        <form className={classes.root} noValidate autoComplete="off">
            <TextField /* Length */
                id="filled-basic"
                defaultValue="Fill"
                value={PortfolioLength}
                variant="filled"
                InputProps={{
                    readOnly: true,
                }}
            />
            
            <TextField /* Current Value */
                id="filled-basic"
                defaultValue="Fill"
                value={CurrentValue}
                variant="filled"
                InputProps={{
                    readOnly: true,
                }}
            />
            <TextField /* Profit or Loss Dollars */
                id="filled-basic"
                defaultValue="Fill"
                value={ProfitLossD}
                variant="filled"
                InputProps={{
                    readOnly: true,
                }}
            />
            <TextField /* Profit or Loss % */
                id="filled-basic"
                defaultValue="Fill"
                value={ProfitLossP}
                variant="filled"
                InputProps={{
                    readOnly: true,
                }}
            />
            <TextField /* Drawdown */
                id="filled-basic"
                defaultValue="Fill"
                value={Drawdown}
                variant="filled"
                InputProps={{
                    readOnly: true,
                }}
            />
            <TextField /* Trades */
                id="filled-basic"
                defaultValue="Fill"
                value={Trades}
                variant="filled"
                InputProps={{
                    readOnly: true,
                }}
            />
        </form>
    );


}