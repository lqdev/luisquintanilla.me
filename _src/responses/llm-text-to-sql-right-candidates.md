---
title: "Using LLM to select the right SQL Query from candidates"
targeturl: https://arxiv.org/abs/2401.02115
response_type: bookmark
dt_published: "2024-04-10 21:59"
dt_updated: "2024-04-10 21:59 -05:00"
tags: ["ai","research","sql","llm"]
---

> Text-to-SQL models can generate a list of candidate SQL queries, and the best query is often in the candidate list, but not at the top of the list. An effective re-rank method can select the right SQL query from the candidate list and improve the model's performance. Previous studies on code generation automatically generate test cases and use them to re-rank candidate codes. However, automatic test case generation for text-to-SQL is an understudied field. We propose an automatic test case generation method that first generates a database and then uses LLMs to predict the ground truth, which is the expected execution results of the ground truth SQL query on this database. To reduce the difficulty for LLMs to predict, we conduct experiments to search for ways to generate easy databases for LLMs and design easy-to-understand prompts. Based on our test case generation method, we propose a re-rank method to select the right SQL query from the candidate list. Given a candidate list, our method can generate test cases and re-rank the candidate list according to their pass numbers on these test cases and their generation probabilities. The experiment results on the validation dataset of Spider show that the performance of some state-of-the-art models can get a 3.6\% improvement after applying our re-rank method. 