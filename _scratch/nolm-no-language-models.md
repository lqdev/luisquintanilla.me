# NoLM - Say no to Language Models

## Why?

Given all the excitement around Language Models, it's fun to be a contrarian.

The name NoLM is a play on NoSQL, which was a reaction to SQL at the time. 

While the scope of the content is focused around language models, much of it is applicable to GenAI models in general.

### Language Models aren't very useful (on their own)

Chances are, when you think of applications that use Language Models, you think ChatGPT. However, while Language Models drive apps like ChatGPT, Language Models are not ChatGPT. Going from Language Model to ChatGPT takes work. 

#### Internal knowledge needs to be augmented

Although the responses a language model can generate without any additional guidance are impressive, it's not able to access real-time information. More importantly, models don't have access to your data which is what makes their responses more useful and relevant. 

Although fine-tuning techniques are driving down the cost in terms of compute required, it's still expensive. 

As a result, you need to augment the knowledge language models posses. 

Various patterns and techniques such as prompt engineering using few-shot prompting, RAG, Advanced RAG and many others which pull from various up-to-date and relevant data sources have emerged. 

As a result, language models are able to generate better responses. 

However, the amount of context language models can handle is limited. Meaning, you generally can't just stuff your entire database in the prompts to help you learn about sales trends. 

This means you need to pick which data is relevant and employ clever retrieval techniques which will only work if you have good data. 

Garbage in, garbage out. 

#### No capability to act on their environment

When people think AI, they generally think of movies like Artificial Intelligence, Terminator, or something in between. 

However, language models and AI in its current state can't act on the environment. They can generate somewhat convicing answers, but they can't actually do anything. At least anything that affects its external environment. 

If you type, "Turn off the lights", into your preferred AI chat assistant, chances are nothing will happen. 

In order for AI to act on its external environment, it needs to be provided with tools, functions, plugins, etc. 

In the lights case, you might give it access to your smart home API and even then, you might need to do some mapping and integration to get it working. 

Similarly, if you ask it to book a trip, add an item to a shopping cart, or order lunch, the language model would need to be integrated with existing APIs and software to make them useful. 

#### Difficult to constrain and standardize outputs

Language models non-deterministic. They are probability based. The next word gets generated based on some sampling strategy and as a result, it's difficult to predict or standardize the output. 

While you can try prompt-engineering techniques such as asking it to respond in a certain format (i.e. XML or JSON), or you may opt for providing a few examples of how you want it to respond (few-shot learning), these are at best suggestions. An additional challenge with the prompt engineering approach is that when you upgrade to newer versions of the models or an entire model altogether, your prompts may no longer be useful or relevant.   

If prompt engineering doesn't work, you may resort to libraries such as TypeChat, Guidance, or DsPy to help out as well. 

In any case, it's a hard problem to predict with a high degree of certainty what outputs may look like. 
 
#### Planning is hard

For relatively complex queries such as "What are the average quarterly sales for the past 2 years?", an application using AI might:

- Translate natural language request to a SQL query.
- Use a tool to run the query against the database.
- Format the query results and generate a response.

For more complex problems though, even when given additional data sources and tools, it may be difficult for the language model to formulate and execute a plan. 

While I don't want to anthropomize Language Models, this is no different from a person solving a really hard problem. Imagine I asked you to, "solve world hunger". 

You:

- May not know where to start
- Might not have the tools at your disposal. Even if you do have a set of tools at your disposal, which ones do you use?
- Might not have all the information you need. Even if you know the right people and have the right information, what's actually relevant and useful to you right now to solve this problem?
- Can't predict whether the plans you make or actions you take actually solve the problem

Given enough time, you may figure it out and everyone will be better for it. Chances are though, you won't solve it in a few seconds or minutes which is probably the reasonable amount of time we expect language models to respond to us (if they even respond or solve the problem).

### Small Language Models (SLMs) aren't any better in some cases

Small Language Models are exciting for so many reasons. However, they still face challenges. In some cases, they're similar to Large Language Models, and in others, they're unique. 

#### Size of Small Language Models

Small Language Models is a misnomer. Small(er) Language Models is more like it. Looking at some of the smaller models like Llama, Phi, and Gemma they're still several GB. Smaller than GPT or any of the Large Language Models, but not small. 

While it's easier to run in more resource-constrained environments, commodity hardware still runs into challenges and specialized hardware like GPUs is still recommended.

In deployment scenarios, particularly cloud-native, there's been a push to shrink the runtimes and container images as much as possible. While the storage and memory footprint of your application may be a few MB, it no longer matters because your container will be at least a few GB because of the model. This could be optimized by mounting volumes and separating storage and compute. However, that could also introduce increased cold-start times and latency. Furthermore, while you may have solved your storage problem, it still needs to run somewhere and memory is something you'll have to also account for. 

#### Small Language Models aren't free?

Small Language Models are free as in beer and maybe even as freedom. Many of them are available in the open, though the definition of [whether they are open-source is still being worked out by the Open Source Initiative (OSI)](https://opensource.org/deepdive). 

The open-source community is amazing and has been a big driver behind Small Language Models. Being that they are available in the open, whether that's HuggingFace or through tools like Ollama, Llamafile, llama.cpp, OnnxRuntime GenAI, LM Studio, and many others, acquiring and using these models generally doesn't incur any cost. In cases where license permits, you may even use these for commercial purposes. 

That said, it's one thing to download these models onto your laptop or as part of your CI/CD pipeline and use them in resource-constrained environments. It's another to operationalize and bring these models to production. When bringing these models to production, you'll have perform actions such as:

- Allocate storage
- Allocate compute (preferably specialized compute like GPUs)
- Add logging and monitoring
- Add Responsible AI and safety features 
- Perform threat modeling and red-teaming
- Maybe fine-tune to tailor to your domain
- Build CI/CD pipelines

None of those is free, so you have to consider Total Cost of Ownership (TCO) in your solution. At that point, are you better off building vs. buying? What are the core competencies of your business? That's not to say you don't also have to perform some of those same tasks for language model services. But many of the items listed are not there or they're at higher levels in the stack. As a result, you can focus on the integrations and delivering value rather than the operations. That's not to say service-based solutions don't have their own set of concerns, but you do have to consider where your time and money are better spent.  

#### Internal knowledge is even more limited

The size of a model tends to be proportional to the knowledge it internalizes. This is what the weights are. A model trained on your company's documents will be significantly smaller than a model trained on the entire internet. This is great from a storage and memory footprint. However, the tradeoff is, it has less experience or exposure to certain patterns. 

Think of it like this. Imagine going to an expert physicist who knows quantum mechanics at a deep level. Then you tell them, explain it like I'm 5. They might be able to simplify the concept for you where even you might be able to explain it to someone else in a coherent way. However, you can't get deeper because your knowledge is limited. This is how techniques like distillation work. You can shrink the size of the model by synthesizing the internal representations. However, detailed knowledge is lost along the way. 

Similarly, let's say that you asked the same physicist to tell you about something they don't know much or anything about. Maybe it's something like making a 7-layer chocolate cake. This is effectively smaller models. They're knowlegeable in some areas but don't have knowledge of everything because the areas / data used to train them was much smaller. 

As a result, the ability to generalize compared to Large Language Models is reduced and as a result so is the quality of the responses they generate. 

#### It's still not useful on its own

In the end, Small Language Models are still Language Models, which means:

- Their knowledge needs to be augmented.
- They need tools to act on their environment
- Their outputs need to be constrained
- Planning is still hard

So you still have to solve for those. 

## What is the alternative?

### Towards smaller units of computation

Similar to how software is designed, chances are you don't have a single function which does everything in your application. Maybe a small script, but the larger and more complex your application, you tend to organize and build software using smaller units of computation which are composed together to achieve an outcome. 

#### Agents and microservices

In the context of AI, these smaller units of computation are agents. 

Instead of having an application which uses a language model to perform complex tasks, you can split up the various concerns and responsibilities into agents. 

Putting it in the context of applications, you can think of agents as microservices. As opposed to monoliths where all of the functionality of an application is built around a single place, microservices:

- Have a well-defined and scoped domain
- Can be built by using a combination of different technologies which more efficiently solve the problem
- Communicate by passing messages

Bringing it back to agents, this means an agent can:

- More effectively complete tasks if it's designed to focus on a single domain.
- Because of a contrained domain, it may be possible to:
    - Use different types of models depending on the problem complexity (LLM, SLM, Classical ML, expert system, etc.), 
    - Use a subset of tools to help it perform actions. If all it needs is to turn off the lights, it doesn't need access to my calendar,
    - Use a subset of data sources scoped to the domain it's responsible for. If its domain is to manage inventory in the warehouse, it doesn't need access to the sales department's presentations. 
- Message passing can leverage the natural language capabilities of language models to more efficiently coordinate with other agents. 

#### Favoring composition and collaboration

By breaking AI applications into smaller units of computation, this now enables you to more easily compose the various components. As you design your agent systems as microservices, you can make it easier for the various agents to discover and communicate with each other in order to collaborate in service of their goal.  

In the classical ML domain, ensemble methods in many cases were effective at solving problems because the concensus of several weak learners. 

Language Model architectures that employ Mixture-of-Experts patterns seem to follow that trend. 

Agent systems are no different and the sum of their parts are greater than the whole. 

### Is that really all it is?

Nope. Looking back at the knowledge we've gained from building microservices over the last few years, it's not a one-size fits all. Separation of concerns and simplified domains make it easy to build complex applications. At the same time, those components and services need to communicate and coordinate with each other which introduces additional complexity.  However, by leaning on what we've learned from microservices and designing our AI applications in a similar manner as well as leveraging similar tools we've used to build those systems, we can increase the likelihood of success when building AI applications.

## Conclusion

In the end, use the right tool for the job. That means, a combination of Language Models, expert systems, classical ML models, even Small(er) Language Models, and agentic patterns. 

In the beginning, I mentioned that the NoLM is a play on NoSQL. 

I've seen various interpretations for what NoSQL means, such as:

- No SQL
- Not Only SQL

I prefer the latter, Not Only SQL. Over time, we've seen SQL databases adopt document datatabase features and document databases adopt SQL database features. Similarly, with the recent rise in vector databases, we're starting to see stores like PostgreSQL, Redis, MongoDB, and others add vector support. In all those cases, those solutions won't perform in the areas they weren't originally designed for. However, it's still handy to have the option available. As long as you understand that you're using a less optimal tool in favor of convenience.

You can look at Language Models in a similar way. They are really good at taking natural language and mapping it to a more structured format. In doing so, they are good generalists that are able to synthesize information and produce responses given the right data sources and tools. However, they are not specialists or experts in one area. More importantly, those specialist and expert systems can sometimes be simpler to build and cheaper to operate. In doing so, language models can be used to map complex tasks into simpler ones. Those simpler tasks can be delegated to simpler rules-based or classical AI based systems. Say for example you want to do intent detection. Instead of having the Language Model go through the process of performing classification, you might use zero-shot classification with smaller models, logistic regression or even clustering. In each of those cases, the models used can be more specialized, smaller in size, quicker to respond, and easier to operate. The applications these models are added to can be designed to follow micro-service and agentic patterns which constrain the domain and areas of responsibilily. In the end, you end up not with a monolith, but a mixture of experts being greater than the whole in helping you use AI to solve problems. 
