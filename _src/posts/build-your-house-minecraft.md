---
post_type: "article" 
title: Design and Build Your Next Home in Minecraft
published_date: 2017-12-15 22:34:12 -05:00
tags: [microsoft, artificial intelligence, thoughts, robots]
---

#### TLDR

Using Minecraft and Mods, virtual representations of the real world can be built to train autonomous agents to navigate and perform tasks in this virtual environment. These virtual models can then be used by physical agents such as Boston Dynamics bots to implement the tasks and instructions learned using the virtual models. Ultimately this makes it reasonable to think of a time where we can design and have our house built in a short time span by autonomous artificial agents.

# Minecraft 

Minecraft is so hard to describe. Is it a game or is it an educational tool? At first I didn’t get it. I watched younger siblings play this game endlessly always finding ways to improve their worlds. Eventually it was bought by Microsoft and since then it has left the core experience relatively untouched. Instead of shelving it away, Microsoft has expanded across more platforms and loaded it with tons of new features such as an education edition and cross-platform play. In Minecraft I have seen individuals build houses, cities, and even rudimentary models of circuits using Redstone. Furthermore, because the project is programmable, individuals can build modifications also known as Mods to extend the default capabilities.

![](/files/images/buildyourhouseminecraft1.png)

# Dynamic Mapping 

A virtual world is great, but what about the real world? Can we model the real world? A few months ago I came across a project by the New York Public Library which generated topographically accurate Minecraft maps using historical map scans and Python scripts. Although the accuracy of historical maps may be questionable, we can only hope that mapping practices have advanced significantly since then and can provide more details and data. Regardless, this project demonstrates that it is possible to model the real world in a virtual environment given the appropriate data. 

![](/files/images/buildyourhouseminecraft2.png)

# Project Malmo

Aside from having the obvious benefit of having a relatively accurate virtual model of the real world, there is the additional benefit of having a starting point for training autonomous agents to navigate this environment. To aid in this endeavor, Project Malmo is an adequate tool. Released to the public in 2015 by Microsoft Research, Project Malmo is a platform for artificial intelligence experimentation and research built on top of Minecraft. The project is cross-platform and agents can be built in Python, Lua, C#, C++ or Java. 

While the benefits of Project Malmo aside from the pedagogical exercise may not be clearly evident, this project is extremely valuable. The cost not only in terms of price but also time to build physical autonomous bots can be high. If for some reason they were to break or malfunction during field tests it can be costly. Therefore a more effective solution might be to simulate the navigation and mobility of these physical autonomous agents in a real world scenario using virtual environments. 

[![Project Malmo](/files/images/buildyourhouseminecraft3.png)](http://www.youtube.com/watch?v=KkVj_ddseO8)

# Manual Labor

Once trained, we just need to find or build the physical agents that will carry out the work and instructions based on training performed in the virtual environments. There have been recent demonstrations of agents that may be able of handling such tasks. One of those is Boston Dynamics. For some time, Boston Dynamics has been creating anthropomorphic and zoomorphic robots that perform amazing feats. It can only be assumed that given a model that has been trained in a real-world environment, these physical agents can perform a task like building a home. 

The thought of a bot building a house is not too abstract. On March of 2017 several reports suggested that a home in Russia was built in under 24 hours using 3-D printing technologies. While the house had a predefined layout and the robotic arm that built it was limited in its mobility, such capabilities could be expanded with the aid of a trained model being executed by sophisticated physical autonomous agents such as those built by Boston Dynamics.

![](/files/images/buildyourhouseminecraft4.png)

###### Sources

[4K Minecraft](https://www.engadget.com/2017/06/14/minecraft-4k-slider-xbox-one-x-e3-microsoft-mojang/)
[Historical Minecraft](https://github.com/NYPL/historical-minecraft/)  
[Project Malmo](https://www.microsoft.com/en-us/research/project/project-malmo/)  
[Boston Dynamics](https://www.bostondynamics.com)  
[House Built in 24 Hours](http://mentalfloss.com/article/92757/3d-printed-house-built-24-hours-russia)
