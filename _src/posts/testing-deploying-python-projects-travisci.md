---
post_type: "article" 
title: Testing and Deploying Python Projects with Travis CI
tags: [devops, travisci, python, ci/cd, programming, development]
published_date: 2018-02-18 20:51:32
---

# Introduction

When working on projects, especially those that others or yourself may depend upon, it is important to test to make sure that everything is working as expected. Furthermore, being able to deploy your code/packages to a central repository with a package manager makes distribution easier. Although this can all be done manually, it can also be automated. Both testing and deployment can be automated using Travis CI which makes the entire process as easy as pushing your most recent changes to GitHub. In this writeup, I will go over how to create a Python project with unit tests and deploy to PyPI. A sample project can be found at this [link](https://github.com/lqdev/TravisTest)

## Prerequisites

- [GitHub Login](https://github.com/)
- [PyPI Login](https://pypi.python.org/pypi)
- [virtualenv](https://virtualenv.pypa.io/en/stable/)

### Install virtualenv

```bash
sudo pip install virtualenv
```

# Create The Project

For the test project, I will create a module that performs adding, subtracting, increment and decrement operations.

## Define Folder Structure

We start out by creating a directory for our project.

```bash
mkdir travistest
```

Inside that directory, we want to have have a directory for our module as well as for our tests. Therefore, we need to create a directory for both of them.

```bash
mkdir travistest
mkdir test
```

Finally, we want to initialize the virtual environment for our project. To do so, we can use the `virtualenv` package. For the project `python 3.5` is the version that will be used.

```bash
virtualenv -p python3.5 ENV
```

After installation, a folder with the name `ENV` should appear in the root directory of the project. The final directory structure should look like so:

```text
travistest
|_ENV
|_travistest
|_test
```

## Install Modules

For this project, I'll be using `pytest` for unit testing. Before installing anything however, I'll need to activate the virtual environment.

```bash
source ENV/bin/activate
```

Once our virtual environment is activated, we can install `pytest`.

```bash
pip install pytest
```

After installation, we can persist installed packages inside a `requirements.txt` file with the `freeze` command.

```bash
pip freeze > requirements.txt
```

## Create The Module

Inside the `travistest` module directory, the easiest way to create a module is to include an `__init__.py` file inside the directory. It's okay if it's empty.

Therefore, we can start by creating the `__init__` file in that directory.

```bash
touch __init__.py
```

Once that's created, we can begin writing the main functionality of our module. Inside a file called `Operations.py`, we can put the following code in.

```python
class Operations:
    def __init__(self):
        pass
    
    def add(self,x,y):
        return x + y

    def subtract(self,x,y):
        return x - y

    def increment(self,x):
        return x + 1

    def decrement(self,x):
    	return x - 1
```

## Unit Test

Once we have our code, we need to write tests for it. Navigating to the `test` directory, we can add the following code to the `test_operations.py` file.

```python
from pytest import fixture

@fixture
def op():
    from travistest.Operations import Operations
    return Operations()

def test_add(op):
    assert op.add(1,2) == 3

def test_subtract(op):
    assert op.subtract(2,1) == 1

def test_increment(op):
    assert op.increment(1) == 2

def test_decrement(op):
assert op.decrement(2) == 1
```

To make sure everything is working correctly, from the project's root directory, we can run the `pytest` command. If all goes well, an output similar to the one below should appear.

```bash
============================= test session starts ==============================
platform linux -- Python 3.5.2, pytest-3.4.0, py-1.5.2, pluggy-0.6.0

collected 4 items                                                              

test/test_operations.py ....                                             [100%]

=========================== 4 passed in 0.04 seconds ===========================
```

# Prepare For Deployment

To prepare for deployment and uploading to PyPI, we need to add a `setup.py` file to the root directory of our project. The contents of this file for our purposes are mostly metadata that will populate information in PyPI.

```python
from distutils.core import setup

setup(
    name='travistest',
    packages=['travistest'],
    version='0.0.7',
    description='Test project to get acquainted with TravisCI',
    url='https://github.com/lqdev/TravisTest',    
)
```

## Setup Travis

### Enable Repository

Assuming that you have a `GitHub` login and a repository has been created for your project:

1. Log into [travis-ci.org](https://travis-ci.org/) with your GitHub credentials.
2. Once all of your repositores are synced, toggle the switch next to the repository containing your project.

### Configure .travis.yml

Once the project has been enabled, we need fo configure Travis. This is all done using the `.travis.yml` file.

In this file we'll tell Travis that the language of our project is Python version 3.5 and that we'll be using a virtual environment. Additionally we'll require sudo priviliges and target the Ubuntu Xenial 16.04 distribution. All of these configurations can be done as follows.

```yaml
sudo: required
dist: xenial
language: python
virtualenv:
  system_site_packages: true
python:
- '3.5'
```

Once that is set up, we can tell it to install all of the dependencies stored in our `requirements.txt` file.

```yaml
install:
- pip install -r requirements.txt
```

After this, we need to tell Travis to run our tests just like we would on our local machine.

```yaml
script: pytest
```

Once our tests have run, we need to make sure we are back in the root directory of our project for deployment.

```yaml
after_script: cd ~
```

We're done with the automated testing script portion of our project. Now we need to setup deployment options. This section will mainly contain the credentials of your PyPI account.

```yaml
deploy:
  provider: pypi
  user: "YOURUSERNAME"
```

After setting the provider and user, we need to set the password. Because this will be a public repository DO NOT enter your password on this file. Instead, we can set an encrypted version that only Travis can decrypt. To do so, while in the root directory of our project, we can enter the following command into the terminal.

```bash
travis encrypt --add deployment.password
```

Type your password into the terminal and press `Ctrl + D`.

If you take a look at your `.travis.yml` file you should see something like the following in your `deploy` options

```bash
deploy:
  provider: pypi
  user: "YOURUSERNAME"
  password:
    secure: "YOURPASSWORD"
```

The final `.travis.yml` file should like like so

```yaml
sudo: required
dist: xenial
language: python
virtualenv:
  system_site_packages: true
python:
- '3.5'
install:
- pip install -r requirements.txt
script: pytest
after_script: cd ~
deploy:
  provider: pypi
  user: "YOURUSERNAME"
  password:
    secure: "YOURPASSWORD"
```

# Deploy

Now that we have everything set up, deployment should be relatively easy. A build is triggered when changes are pushed to the repository on GitHub. Therefore, pushing your local changes to the remote GitHub repository should initialize the build. To track progress, visit the project's page on Travis CI.

**NOTE: WHEN PUSHING NEW CHANGES, INCREMENT VERSION NUMBER IN THE `SETUP.PY` FILE SO THAT EXISTING FILE ERRORS DO NOT CRASH BUILDS**

# Conclusion

In this writeup, we created a Python package that performs basic operations and set up automated testing and deployment with Travis CI. Configurations can be further customized and refined to adapt more complex build processes.