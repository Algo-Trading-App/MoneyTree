import React, { Component } from 'react';
import TextField from '@material-ui/core/TextField';
import Button from '@material-ui/core/Button';
import Api from '../Api';
import './Styling.css';

export class UserData extends Component {
    static displayName = UserData.name;
    // Create state for each variable
    constructor(props) {
        super(props);
        this.state = {
            FirstName: '',
            LastName: '',
            Email: '',
        };

        this.fnameChange = this.fnameChange.bind(this);
        this.lnameChange = this.lnameChange.bind(this);
        this.emailChange = this.emailChange.bind(this);
        this.handleSubmit = this.handleSubmit.bind(this);
    }
    // When user inputs, change state to represent that current input
    fnameChange(event) {
        this.setState({ FirstName: event.target.value });
    }
    lnameChange(event) {
        this.setState({ LastName: event.target.value });
    }
    emailChange(event) {
        this.setState({ Email: event.target.value });
    }

    // Call api to POST user data to DB
    handleSubmit(event) {
        //Api class Function to send User data
        Api.postUserData(this.state.FirstName, this.state.LastName, this.state.Email);
        event.preventDefault();
    }

    render() {
        return (
            <React.Fragment>
                <form onSubmit={this.handleSubmit}>
                    <div>
                        <header> 
                            <h1> Personal Information
                                <div>
                                <TextField id="standard-basic" label="First Name" onChange={this.fnameChange} value={this.state.fname} />
                                <div class="divider" />
                                <TextField id="standard-basic" label="Last Name" onChange={this.lnameChange} value={this.state.lname} />
                                <div class="divider" />
                                <TextField id="standard-basic" label="Email" onChange={this.emailChange} value={this.state.email} />
                                </div>
                            </h1>
                        </header>


                    </div>
                    <Button type="submit" variant="contained" color="primary">
                        Submit
                    </Button>
                </form>
            </React.Fragment>
        );
    }
}