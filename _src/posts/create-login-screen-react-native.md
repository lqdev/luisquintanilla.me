---
post_type: "article" 
title: Creating A Login Screen With React Native
published_date: 2017-12-09 13:15:57 -05:00
tags: [react,react native, javascript]
---

This assumes that you have Node and NPM installed on your PC. If not, you can go to the official website and install them. Then, come back to this page.

# Part I – Install React-Native

```bash
npm install -g create-react-native-app
```

# Part II – Create and Run Project

```bash 
create-react-native-app myfirstreactnativeapp
cd myfirstreactnativeapp
npm start
```

# Part III – Create Login Screen

In the `App.js` file that is created, we’re going to want to edit a few details.

At the top of the file, add the following line of code to import the visual components we will utilize to create the page.

```javascript
Import { TextInput, StyleSheet, Button } from 'react-native';
```

Inside of your `App` class, you want to add a few things. The first would be to add a constructor for the component. This will allow us to define and keep track of the text input being provided by the user.

```javascript
constructor(props) {
  super(props);
  this.state = {
    name: "",
    email: "",
    password: ""
  }
}
```

Then, we want to update our render method to show the text inputs and submit button.

```javascript
render(){
  return (
    <View>
      <TextInput
        style={styles.input}
        onChangeText={(text) => this.setState({name: text})}
        placeholder="Name"
      />
      <TextInput
        style={styles.input}
        onChangeText={(text) => this.setState({email: text})}
        placeholder="E-Mail"
      />
      <TextInput
        style={styles.input}
        onChangeText={(text) => this.setState({password:text})}
        secureTextEntry={true}
        placeholder="Password"
      />
      <Button
        onPress={this.submit}
        title="Submit"
        color="#841584"
      />
    </View>
  )
}
```

Using the `StyleSheet` component, we’ll also want to add some styling to our inputs. We can do so by adding the following lines of code outside of our App class definition. These styles are applied as properties of the `TextInput` components inside the App class.

```javascript
const styles = StyleSheet.create({
  input: {
    width: 250,
    margin: 5
  }
});
```

Finally, we need to create a submit method to handle the press event on the submit button. I have left it blank, since how you pass the e-mail and password is entirely up to you (although hopefully it is in the body of an HTTP Request as opposed to url parameters).

```javascript
submit() {
//Do Something
}
```
