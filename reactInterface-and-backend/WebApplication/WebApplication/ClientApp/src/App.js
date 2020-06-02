import React from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { Home } from './components/Home';
import Portfolio from './components/Portfolio';
import { useAuth0 } from "./react-auth0-spa";
import { UserData } from './components/UserData';
import Profile from "./components/Profile"
import { Test } from "./components/Test"
import history from "./utils/history";
import { Router } from "react-router-dom";
//import PrivateRoute from "./components/PrivateRoute";

import './custom.css'

function App() {
    const { loading } = useAuth0();

    if (loading) {
        return <div>Loading...</div>;
    }
    return (
        <Router history={history}>
            <Layout>
                <Route exact path='/' component={Home} />
                <Route path='/portfolio' component={Portfolio} />
                <Route path='/user-data' component={UserData} />
                <Route path='/profile' component={Profile} />
                <Route path="/test" component={Test} />
            </Layout>
        </Router>
    );
}

export default App;


