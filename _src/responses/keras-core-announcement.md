---
title: "Introducing Keras Core: Keras for TensorFlow, JAX, and PyTorch."
targeturl: https://keras.io/keras_core/announcement/
response_type: bookmark
dt_published: "2023-07-11 16:22"
dt_updated: "2023-07-11 16:22 -05:00"
tags: ["deeplearning","ai","keras","jax","tensorflow","pytorch"]
---

> We're excited to share with you a new library called Keras Core, a preview version of the future of Keras. In Fall 2023, this library will become Keras 3.0. Keras Core is a full rewrite of the Keras codebase that rebases it on top of a modular backend architecture. It makes it possible to run Keras workflows on top of arbitrary frameworks — starting with TensorFlow, JAX, and PyTorch.
> 
> Keras Core is also a drop-in replacement for tf.keras, with near-full backwards compatibility with tf.keras code when using the TensorFlow backend. In the vast majority of cases you can just start importing it via import keras_core as keras in place of from tensorflow import keras and your existing code will run with no issue — and generally with slightly improved performance, thanks to XLA compilation.