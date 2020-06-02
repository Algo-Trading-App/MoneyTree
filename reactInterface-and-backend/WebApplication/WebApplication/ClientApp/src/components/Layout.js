import React, { Component } from 'react';
import './Styling.css';

import  NavMenu from './NavMenu';

export class Layout extends Component {
  static displayName = Layout.name;

  render () {
    return (
        <div>
            <NavMenu />
            <div className="page">
                {this.props.children}
            </div>
      </div>
    );
  }
}
