import React from 'react';
import { Link } from 'react-router-dom';
import { makeStyles } from '@material-ui/core/styles';
import AppBar from '@material-ui/core/AppBar';
import Toolbar from '@material-ui/core/Toolbar';
import Button from '@material-ui/core/Button';
import { useAuth0 } from "../react-auth0-spa";
import { Typography } from '@material-ui/core';

const useStyles = makeStyles(theme => ({
    root: {
        flexGrow: 1,
    },
    title: {
        flexGrow: 1,
    },
}));

const NavMenu = () => {
    const classes = useStyles();
    const { isAuthenticated, loginWithRedirect, logout } = useAuth0();
    //Navbar for user navigation of pages
    return (
        <div className={classes.root}>
            <AppBar position="static">      
                <Toolbar>
                    <Typography variant="h6" className={classes.title}>
                        Auto Stock Investments
                    </Typography>
                    <Button component={Link} color="inherit"  to="/">Home</Button>
                    <Button component={Link} color="inherit" to="/portfolio">Portfolio</Button>
                    <Button component={Link} color="inherit" to="/Test">Test</Button>
                    { isAuthenticated && (<Button component={Link} color="inherit" to="/user-data">UserData</Button>) }
                    { isAuthenticated && (<Button component={Link} color="inherit" to="/profile">Profile</Button>) }
                    {!isAuthenticated && ( <Button color="inherit" onClick={() => loginWithRedirect()}>Login</Button> )}
                    {isAuthenticated && (<Button color="inherit" onClick={() => logout()}>Logout</Button>)}
                </Toolbar>
            </AppBar>
        </div>
    );
}

export default NavMenu;