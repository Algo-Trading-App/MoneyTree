import TableBody from '@material-ui/core/TableBody';
import TableHead from '@material-ui/core/TableHead';
import TableRow from '@material-ui/core/TableRow';
import TableCell from '@material-ui/core/TableCell';
import PropTypes from 'prop-types';
import React from 'react';
import Typography from '@material-ui/core/Typography';
import Box from '@material-ui/core/Box';
import { makeStyles, withStyles } from '@material-ui/core/styles';
import Table from '@material-ui/core/Table';
import Button from '@material-ui/core/Button';

const useStyles = makeStyles(theme => ({ // Styling for material-ui components
    root: {
        flexGrow: 0,
    },
    table: {
        minWidth: 0,
        overflow: "hidden"
    },
    formControl: {
        margin: theme.spacing(1),
        minWidth: 120,
    },
    selectEmpty: {
        marginTop: theme.spacing(2),
    },
    tabs: {
        flexGrow: 0,
        backgroundColor: theme.palette.background.paper,
    },
}));

// Creates the porfolio entries and changes risk value to be non-integer value for easier clarification to user
function createPortfolio(id, name, value, risk) { 
    if (risk === 0) {
        risk = 'Low';
    }
    if (risk === 1) {
        risk = 'Medium';
    }
    if (risk === 2) {
        risk = 'High';
    }
    return { id, name, value, risk };
}

// Styling function for material-ui table component
const StyledTableCell = withStyles(theme => ({
    head: {
        backgroundColor: theme.palette.common.black,
        color: theme.palette.common.white,
    },
    body: {
        fontSize: 14,
    },
}))(TableCell);

// Styling function for material-ui table component
const StyledTableRow = withStyles(theme => ({
    root: {
        '&:nth-of-type(odd)': {
            backgroundColor: theme.palette.background.default,
        },
    },
}))(TableRow);

// Styling function for material-ui table component
function TabPanel(props) {
    const { children, value, index, ...other } = props;

    return (
        <Typography
            component="div"
            role="tabpanel"
            hidden={value !== index}
            id={`simple-tabpanel-${index}`}
            aria-labelledby={`simple-tab-${index}`}
            {...other}
        >
            {value === index && <Box p={3}>{children}</Box>}
        </Typography>
    );
}
TabPanel.propTypes = {
    children: PropTypes.node,
    index: PropTypes.any.isRequired,
    value: PropTypes.any.isRequired,
};

// Portfolio Table component
// Inputs: activePortfolio, setActivePortfolio react states, and table data
// ...props is for lifting states up
export function PortfolioTableWrapper({ setPortfolioId, setCurrentPortfolioLength, tableData, ...props }) {
    const classes = useStyles();
    const [value] = React.useState(0);

    const portfolio = []; // const array to hold all the portfolio data.
    for (let i = 0; i < tableData.length; i++) { // Add all the portfolio in DB to array to display to user later 
        portfolio.push(createPortfolio(i, "Portfolio " + i, tableData[i].initialValue, tableData[i].desiredRisk));
    }

    function Clicked(id) {
        setCurrentPortfolioLength(tableData[id].holding.length);
        setPortfolioId(id);
    }

    return (
        <div className={classes.tabs}>
            <TabPanel value={value} index={0}>
                <Table className={classes.table} size="small" aria-label="customized table">
                    <TableHead>
                        <TableRow  /* Titles for each column */>
                            <StyledTableCell>Name</StyledTableCell>
                            <StyledTableCell align="right">Risk</StyledTableCell>
                            <StyledTableCell align="right">Value</StyledTableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {portfolio.map(row => ( // Iterate over portfolio array and displays each as a row in a table
                            <StyledTableRow key={row.id}>


                                <StyledTableCell component="th" scope="row">
                                    <Button color="primary" variant="contained" onClick={() => Clicked(row.id)/* Sets portfolio state on user selected row */} >
                                        {row.name}
                                    </Button>
                                </StyledTableCell>
                                <StyledTableCell align="right">{row.risk}</StyledTableCell>
                                <StyledTableCell align="right">{row.value}</StyledTableCell>
                            </StyledTableRow>
                        ))}
                    </TableBody>
                </Table>
            </TabPanel>
        </div>
    );
}