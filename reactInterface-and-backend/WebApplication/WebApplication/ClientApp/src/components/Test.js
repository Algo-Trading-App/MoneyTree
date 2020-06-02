import React, { Component } from 'react';
import TextField from '@material-ui/core/TextField';
import Button from '@material-ui/core/Button';
import Api from '../Api';
import './Styling.css';

export class Test extends Component {
    static displayName = Test.name;
    constructor(props) {
        super(props);
        this.state = {
            Email: '',
        };

        this.emailChange = this.emailChange.bind(this);
        this.handleSubmit = this.handleSubmit.bind(this);
    }
    emailChange(event) {
        this.setState({ Email: event.target.value });
    }

    handleSubmit(event) {
        //Api class Function to send User data
        Api.getPortfolio(this.state.Email);
        event.preventDefault();
    }


    render() {
        return (
            <React.Fragment>
                <form onSubmit={this.handleSubmit}>
                    <div>
                        <header>
                            <h1> Test
                                <div>
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