open System
open System.IO
open System.Net.Http
open System.Net.Http.Json

let client = new HttpClient()
client.BaseAddress <- new Uri("http://localhost:11434")
client.Timeout <- TimeSpan.FromMinutes(5)

let dir = Path.Join("/","workspaces","luisquintanilla.me","_src","responses","decoder-nilay-patel-why-websites-are-the-future.md")

let filecontent = File.ReadAllText(dir)

let prompt = 
    $"""
    Generate at least 10 hashtags for the following blog post. 
    
    Format as a JSON array

    For multiple words, make them one word. For example "artificial intelligence" would be #artificialintelligence

    {filecontent}
    """

let body = 
    {|
        model="phi"
        messages=[|
            {|role="system"; content="You are an AI assistant who helps authors generate tags for their articles"|}
            {|role="user"; content=prompt|}
            
        |]
        stream=false
    |}

let req = 
    task {
        let! res = client.PostAsJsonAsync("/v1/chat/completions", body)
        let! content = res.Content.ReadAsStringAsync()
        return content
    }

req
|> Async.AwaitTask
|> Async.RunSynchronously
|> printfn "%s"