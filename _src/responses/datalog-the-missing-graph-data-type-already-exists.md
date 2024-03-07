---
title: "The "missing" graph datatype already exists. It was invented in the '70s"
targeturl: https://tylerhou.com/posts/datalog-go-brrr/
response_type: reshare
dt_published: "2024-03-06 21:39"
dt_updated: "2024-03-06 21:39 -05:00"
tags: ["datalog","programming","graphs","datatypes"]
---

> The datatype for a graph is a relation, and graph algorithms are queries on the relation. But modern languages need better support for the relational model.

> This post is a response to/inspired by [The Hunt for the Missing Data Type (HN) by Hillel Wayne](https://www.hillelwayne.com/post/graph-types/). I suggest reading his article first.

> I claim the reason why it is so difficult to support graphs in languages nowadays is because the imperative/structured programming model of modern programming languages is ill-suited for graph algorithms. As Wayne correctly points out, the core problem is that when you write a graph algorithm in an imperative language like Python or Rust, you have to choose some explicit representation for the graph. Then, your traversal algorithm is dependent on the representation you chose. If you find out later that your representation is no longer efficient, it is a lot of work to adapt your algorithms for a new representation.  
> <br>
> So what if we just, like, didn’t do this?  
> <br>
> We already have a declarative programming language where expressing graph algorithms is extremely natural—Datalog, whose semantics are based on* the relational algebra, which was developed in the 1970s.

> Wonderful! Except for the “writing Datalog” part.  
> <br>
> If Datalog is so great, why hasn’t it seen more adoption?  
> <br>
> The short answer is that Datalog is relatively esoteric outside of academia and some industry applications and, as a result, is not a great language from a “software engineering” perspective. It is hard for programmers accustomed to imperative code to write Datalog programs, and large Datalog programs can be hard to write and understand.