import axios from 'axios';

export default class Api {

    static setAuthorizationHeader(jwt) {
        axios.defaults.headers.common["Authorization"] = "Bearer " + jwt;

        // Let's also setup the default response for when a 401 is received
        axios.interceptors.response.use(undefined, function (error) {
            if (error.response.status === 401) {
                // TODO: Instead of just navigating back to the login page,
                // could there be a way to show a modal to the user so they can
                // re-authenticate right there and then continue on with what
                // they were doing. This would be a bit of work, so for now
                // simply redirecting to the login page will suffice.
                window.location.href = "/login";
            }
        });
    }

    // Post user inputed data to DB 
    static postUserData(FirstName, LastName, Email) {
        axios.post(`/api/UDM/newuser`, { FirstName, LastName, Email })
            .then(res => {
            });
    }

    static getPortfolio(Email) {
        axios.get(`/api/UDM/portfolio?email=${Email}`)
            .then(function (response) {
                // handle success
                console.log(response.data);
                return response.data;
            })
            .catch(function (error) {
                // handle error
                console.log(error);
            })
            .then(function () {
                // always executed
            });
    }

    //API call to get portfolio data to display to user
    static async getPortfolioTable() {
        try {
            let response = await axios.get(`/api/UDM/portfolio?email=${''}`);
            return response.data;
        } catch (error) {
            console.error(error);
            return null;
        }
    }


    static buyPortfolio(Portfolio) {
        const config = {
            method: 'get',
            url: `/api/UDM/buyportfolio?portfolio=${Portfolio}`
        }
        console.log(Portfolio)
        axios(config)
            .then(function (response) {
                // handle success
                return response;
            })
            .catch(function (error) {
                // handle error
                console.log(error);
            })
            .then(function () {
                // always executed
            });
    }


    // Testing API call
    static getTest(Email) {
        axios.get(`/api/UDM/user?email=${Email}`)
            .then(function (response) {
                // handle success
                console.log(response);
            })
            .catch(function (error) {
                // handle error
                console.log(error);
            })
            .then(function () {
                // always executed
            });

    }
}
