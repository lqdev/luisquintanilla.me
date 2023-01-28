---
title: Serverless Racket Applications Using Azure Functions Custom Handlers
published_date: 2020-03-21 13:45:43
tags: [serverless,racket,lisp,azure-functions,azure,programming,web]
---

## Introduction

[Racket](https://racket-lang.org/) is a fun and powerful general purpose programming language based on the Scheme dialect of Lisp that provides tools and packages that allow individuals to quickly be productive. Although you can build traditional web applications with it, it would be nice to use it with cloud-native technologies like serverless. Azure Functions is designed for these types of event-driven workflows but unfortunately does not officially support Racket. Recently, a new feature called custom handlers was announced which allows individuals to run web applications written in any language that supports HTTP primitives as an Azure Function. When I learned of this feature, my immediate thought was, challenge accepted!

Custom handlers require the following:

1. Write a web server to process requests
2. Define the bindings for the request and response function payloads
3. Configure the Azure Functions host to send request to the web server

In this writeup, I'll show how to set up a Racket web server that processes `GET` requests running as an Azure Function. The source code for this project can be found in the [RacketAzureFunctionsCustomHandlerSample GitHub repository](https://github.com/lqdev/RacketAzureFunctionsCustomHandlerSample)

## Prerequisites

This project was built on a Windows 10 PC, but it should work cross-platform on Mac and Linux.

- [Node.js](https://nodejs.org/en/)
- [Racket](https://download.racket-lang.org/)
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local). This sample uses v2.x of the tool.

## Create Azure Functions project

Create a new directory called `RacketAzureFunctionsCustomHandlerSample` and navigate to it.

## Create Racket server

The way Azure Functions custom handlers work is by having the Azure Functions host proxy requests to a web server written in the language of choice which processes the request and sends the response back to the Azure Functions host.

Start by setting the environment variable where Azure Functions and the server listen on. Create and environment variable called `FUNCTIONS_HTTPWORKER_PORT`. In this example, I set the variable to `7071`.

Inside of your application directory, create a file called *server.rkt* which will contain the server logic.

Open the *server.rkt* file. Define the language and import the required packages.

```racket
#lang racket
(require json)
(require web-server/servlet)
(require web-server/servlet-env)
```

Then, get the port where the server listens on from the `FUNCTIONS_HTTPWORKER_PORT` environment variable.

```racket
(define PORT (string->number (getenv "FUNCTIONS_HTTPWORKER_PORT")))
```

Next, create a function called `get-values` to process your request. In this case, the function receives a `GET` request that returns a JSON object containing a list of integers.

```racket
(define (get-values req)
    (response/full
        200
        #"OK"
        (current-seconds)
        #"application/json;charset=utf-8"
        empty
        (list (jsexpr->bytes #hasheq((value . (1 2 3)))))))
```

After that, define the routes so your server dispatches requests to the appropriate endpoint. In this case, `GET` requests to the `/values` endpoint are sent to and processed by the `get-values` function.

```racket
(define-values (dispatch req)
    (dispatch-rules
        [("values") #:method "get" get-values]
        [else (error "Route does not exist")]))
```

The final *server.rkt* file should contains content similar to the one below:

```racket
;; Define language and import packages
#lang racket
(require json)
(require web-server/servlet)
(require web-server/servlet-env)

;; Get port where server listens on
(define PORT (string->number (getenv "FUNCTIONS_HTTPWORKER_PORT")))

;; Create function to handle GET /values request
(define (get-values req)
    (response/full
        200
        #"OK"
        (current-seconds)
        #"application/json;charset=utf-8"
        empty
        (list (jsexpr->bytes #hasheq((value . (1 2 3)))))))

;; Define routes
(define-values (dispatch req)
    (dispatch-rules
        [("values") #:method "get" get-values]
        [else (error "Route does not exist")]))

;; Define and start server
(serve/servlet
    (lambda (req) (dispatch req))
    #:launch-browser? #f
    #:quit? #f
    #:port PORT
    #:servlet-path "/"
    #:servlet-regexp #rx"")
```

## Test the Racket server

Start the server by running the following command:

```bash
racket --require server.rkt
```

Then, using an application like Postman or Insomina, make a `GET` request to `http://localhost:7071/values`.

The response should look like the following:

```json
{
    "value": [
        1,
        2,
        3
    ]
}
```

## Define function bindings

The way Azure Functions discovers functions is through subdirectories containing a binding definition called *function.json*. The name of the subdirectories must match the name of your function's route path. For example if the route path is `/values`, then the name of the subdirectory is  `values`.

Create a subdirectory inside the main application directory called *values*

Inside the *values* subdirectory, create a file called *function.json* and add the following content to it.

```json
{
    "bindings": [
      {
        "type": "httpTrigger",
        "direction": "in",
        "name": "req",
        "methods": ["get"]
      },
      {
        "type": "http",
        "direction": "out",
        "name": "res"
      }
    ]
}
```

The *function.json* file defines the request and response payloads. In this case, the incoming request is an `HttpTrigger` that only handles `GET` requests and returns an HTTP response.

## Create server executable

Package your server application into a single executable by entering the following command into the command prompt:

```bash
raco exe server.rkt
```

Once your application is packaged, an executable with the name *server.exe* should be created in your application directory.

## Configure Azure Functions host

In your application directory, create a file called *host.json* and add the following contents:

```json
{
  "version": "2.0",
  "httpWorker": {
      "description": {
          "defaultExecutablePath": "server.exe"
      }
  }
}
```

This *host.json* configuration file tells the Azure Functions host where to find the web server executable.

## Run the Azure Functions application

Inside the root application directory, enter the following command into the command prompt.

```bash
func start
```

Using an application like Postman or Insomnia, make a `GET` request to `localhost:7071/api/values`.

The response should look like the following:

```json
{
    "value": [
        1,
        2,
        3
    ]
}
```

## Conclusion

In this writeup, I showed how to create a Racket serverless application that runs on Azure Functions by using custom handlers. Doing so requires you to:

1. Write a web server to process requests
2. Define the bindings for the request and response function payloads
3. Configure the Azure Functions host to send request to the web server

Although in this example, the server was written in Racket, the same process is applicable to other languages. Keep in mind that at the time of this writing, custom handlers are preview and may change. Happy coding!

## Resources

- [Azure Functions Custom Handlers](https://docs.microsoft.com/azure/azure-functions/functions-custom-handlers)