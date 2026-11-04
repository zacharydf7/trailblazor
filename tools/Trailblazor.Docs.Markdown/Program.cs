using Trailblazor.Docs.Markdown;

// Writes the docs site's pages as Markdown, with llms.txt and llms-full.txt, into the published site:
// dotnet run --project tools/Trailblazor.Docs.Markdown -- publish/wwwroot
if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: <the published site's wwwroot folder>");
    return 1;
}

await DocsMarkdown.WriteAsync(args[0]);
return 0;
