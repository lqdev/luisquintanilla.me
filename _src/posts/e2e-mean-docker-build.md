---
post_type: "article" 
title: E2E Dockerizing a MEAN Stack Application
tags: [devops,MEAN,mongodb,docker,angular,angularjs,nodejs,javascript,programming,development,docker-compose]
published_date: 2018-04-29 20:10:09
---


## Introduction

Lately I've been getting familiar with [Docker](https://docker.com). I have built single container applications using a `Dockerfile` and run them locally. This works fine, especially for deployment purposes to a production VM but it's no different than setting up the VM with all the required dependencies and pushing updates via FTP or source control. However, one of the features that I have found extremely useful is multi-container building and deployment via `docker-compose`. With `docker-compose`, not only can I build and run my application, but also dependent services like databases, caches, proxies, etc. Best of all, the builds are standardized and initialized at once without having to individually install the dependencies and components. This writeup explores how to containerize a MEAN stack application and set up a `docker-compose.yml` file for it to build and start the server and database services defined within it.

## Requirements

This writeup assumes that `Docker`, `Docker-Compose` and `Node` are installed on your PC. 

## The Application

The application is a CRUD todo MEAN stack application. The repo for this application can be found [here](https://github.com/lqdev/todomeandockerdemo).

### Project Structure

```text
|_models (Mongoose models)
| |_todo.model.js
|_public
| |_scripts
| | |_controllers
| | | |_main.controller.js
| | |_services
| |   |_todo.service.js
| |_views
| | |_main.html
| |_app.js (front-end application)
| |_index.html
|_Dockerfile (server service)
|_docker-compose.yml
|_api.js (todo api routes)
|_config.js
|_server.js (back-end application)
```

The front-end is built with `AngularJS` and the back-end is built with `NodeJS` using the `Express` web framework and `MongoDB` database. `MongoDB` models are defined with the `Mongoose` package. In the application, users can create, view, update and delete todo tasks. The `Dockerfile` is used to define the container for the web application and the `docker-compose.yml` defines both the `MongoDB` database container as well as the web application container defined in the `Dockerfile`.

## The Docker File

```docker
#Define base image
FROM node:8

#Set Working Directory
WORKDIR /app

#Copy pakage.json file from current directory to working directory
ADD package.json /app

#Install npm packages
RUN npm install

#Copy all application files from local directory to working directory
ADD . /app

#Open port where app will be listening
EXPOSE 3000

#Start application
CMD ['npm','start']
```

### Define Docker Image and Application Directory

The `Dockerfile` has no extension and the syntax is like a standard text file. `#` characters denote comments. `Docker` works based off images which are basically pre-built packages that are stored in one of many registries such as `DockerHub`. `DockerHub` can be thought of as a package repository/manager like `npm` or `dpkg`. In the first two lines of the file we define which base image we want to create our container with. Since this is a MEAN stack application built entirely in `JavaScript`, we'll be using the `node` version 8 image. Then we want set the directory in which our application will reside. We do this by using the `WORKDIR` command and setting `/app` as our application directory, but any directory of your choosing is valid.

### Install Dependecies

All of our dependencies should be defined in our `package.json` file. In order to install these dependencies in our container, we need to copy our local `package.json` file into our container application directory. This can be done with the `ADD` command by passing the `package.json` and application directory `/app` as arguments. Once that file has been copied, it's time to install the dependencies. To run commands through the build process of the application we use the `RUN` command. The command is no different than the one you'd use on your local machine. Therefore, to install the dependencies defined in the `package.json` file we use the command `RUN npm install`.

### Copy Application Files

Once our dependencies are installed, we need to copy the rest of the files in our local project directory to our container application directory. Like with the `package.json` file we use the `ADD` command and pass `.` and `/app` as our source and destination arguments respectively.

#### .dockerignore

Something to keep in mind is that locally we have a `node_modules` directory containing our installed dependencies. In the previous step, we ran the `npm install` command which will create the `node_modules` directory inside our container. Therefore, there is no need to copy all these files over. Like `git`, we can set up a `.dockerignore` file which will contain the files and directory to be ignored by `Docker` when packaging and building the container. The `.dockerignore` file looks like the following.

```text
node_modules/*
```

### Opening Ports

Our application will be listening for connections on a port. This particular application will use port 3000. We need to define the port to listen on in the `Dockerfile` as well. To do so, we'll use the `EXPOSE` command and pass the port(s) that the application will listen on. (MongoDB listens on 27017, but since the `Dockerfile` only deals with the web application and not the database we only need to specify the web application's port).

### Starting the Application

After our container is set up, dependencies are installed and port is defined, it's time to start our application. Unlike the process of running commands while building the container using the `RUN` command, we'll use the `CMD` command to start our application. The arguments accepted by this are an array of strings. In this case, we start our application like we would locally by typing in `npm start`. The `Dockerfile` command to start our application is the following `CMD ['npm','start']`.

## The docker-compose.yml File

```yaml
version: '2'
services:
  db:
    image: mongo
    ports: 
      - 27017:27017
  web:
    build: .
    ports:
      - 3000:3000
    links:
      - "db"
```

The `docker-compose.yml` file is a way of defining, building and starting multi-container applications using `docker-compose`. In our case we have a two container application, one of the containers is the web application we defined and built in the `Dockerfile` and the other is a `MongoDB` database. The `docker-compose.yml` file can take many options, but the only ones we'll be using are the `version` and `services` option. The `version` option defines which syntax version of the `docker-compose.yml` file we'll be using. In our case we'll be using version 2. The `services` option defines the individual containers to be packaged and initialized.

### Services

As mentioned, we have two containers. The names of our containers are `web` and `db`. These names can be anything you want, as long as they're descriptive and make sense to you. The `web` container will be our web application and the `db` container will be our `MongoDB` database. Notice that we have listed our `db` service first and then our `web` service. The reason for this is we want to build and initialize our database prior to our application so that by the time that the web application is initialized, it's able to successfully connect to the database. If done the other way around, an error will be thrown because the database will not be listening for connections and the web application won't be able to connect. Another way to ensure that our database is initialized prior to our web application is to use the `links` option in our `web` service and add the name of the database service `db` to the list of dependent services. The `ports` option like in our `Dockerfile` defines which ports that container will need to operate. In this case, our `web` app listens on port 3000 and the `db` service will listen on port 27017.

#### Container Images

The `docker-compose.yml` can build containers based on images hosted in a registry as well as those defined by a `Dockerfile`. To use images from a registry, we use the `image` option inside of our service. Take the `db` service for example. `MongoDB` already has an image in the `DockerHub` registry which we will use to build the container. Our `web` container does not have an image that is listed in a registry. However, we can still build an image based off a `Dockerfile`. To do this, we use the `build` option inside our service and pass the directory of the respective `Dockerfile` containing the build instructions for the container. 

## Building and Running Containers

Now that our container definitions and files are set up, we're ready to build and run our application. This can be done by typing `docker-compose up -d` in the terminal from inside our local project directory. The `-d` option runs the command detached allowing us to continue using the terminal. This command will both build and start our containers simultaneously. Once the `web` and `db` containers are up and running, we can visit `http://localhost:3000` from our browser to view and test our application. To stop the application, inside the local project directory, we can type `docker-compose stop` in our terminal to stop both containers.

## Conclusion

This writeup uses a pre-configured MEAN stack CRUD todo application and explores how to define a single container `Dockerfile` as well as a multi-container application using `docker-compose`. Docker streamlines how applications are built and deployed while `docker-compose` allows more complex multi-container applications to be orchestrated, linked, deployed and managed simultaneously allowing developers to spend more time  developing solutions and less time managing infrastructure and dependencies.

###### Links/Resources

[Docker Community Edition](https://www.docker.com/community-edition#/download)  
[Docker: Getting Started](https://docs.docker.com/get-started/)  
[NodeJS](https://nodejs.org/en/)