// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
open System.Net
open System.Net.Sockets
open System.IO
open System.Text.RegularExpressions
open System.Text

/// A table of mime content types
let mimeTypes =
    dict [".html", "text/html";
          ".htm" , "text/html";
          ".txt", "text/plain";
          ".gif", "image/gif";
          ".jpg", "image/jpeg";
          ".png", "image/png"]

/// Compute a MIME type from a file expression
let getMimeType(ext) =
    if mimeTypes.ContainsKey(ext) then mimeTypes.[ext]
    else "binary/octet"

/// The pattern Regex1 uses a regular expression to match one elemet
let (|Regex1|_|) (pattern: string) (input: string) =
    try Some(Regex.Match(input, pattern).Groups.Item(1).Captures.Item(0).Value)
    with _ -> None

/// The root for the data we serve
let root = @"home/F-sharp/wwwroot"

/// Handle a TCP connection for an HTTP GET request
let handleRequest (client: TcpClient) (port: int) =
    async {
        use stream = client.GetStream()
        use out = new StreamWriter(stream)
        let sendHeaders (lines: seq<string>) =
            let printLine = fprintf out "%s\r\n"
            Seq.iter printLine lines
            // An empty line is required before content, if any
            printLine ""
            out.Flush()
        let notFound() = sendHeaders ["HTTP/1.0 404 Not Found"]
        let input = new StreamReader(stream)
        let request = input.ReadLine()
        match request with
        // Requests to root are redirected to the start page
        | "GET / HTTP/1.0" | "GET / HTTP/1.1" ->
            sendHeaders <|
                [
                    "HTTP/1.0 302 Found"
                    sprintf "Location: http://localhost:%d/iisstart.htm" port

                ]
        | Regex1 "GET /(.*?) HTTP/1\\.[01]$" fileName ->
            let fname = Path.Combine(root, fileName)
            let mimeType = getMimeType(Path.GetExtension(fname))
            if not <| File.Exists(fname) then notFound()
            else
                let content = File.ReadAllBytes fname
                sendHeaders <| 
                    [
                         "HTTP/1.0 200 OK";
                         sprintf "Content-Length: %d" content.Length;
                         sprintf "Content-Type: %s" mimeType
                    ]
                stream.Write(content, 0, content.Length)
        | _ -> notFound()
    }

/// The server is an asynchronous process, so requests are bundled sequentially
let server =
    let port = 8090
    async {
        let socket = new TcpListener(IPAddress.Parse("127.0.0.0"), port)
        socket.Start()
        while true do
            use client = socket.AcceptTcpClient()
            do! handleRequest client port
    }

    


//[<EntryPoint>]
//let main argv = 
//    printfn "%A" argv
//    0 // return an integer exit code

