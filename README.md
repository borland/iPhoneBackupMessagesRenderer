# What does this do?

You give it the Messages and AddressBook databases from an iPhone backup, and it spits out a directory full of HTML files. Each HTML file is one of your chat messages, styled to look somewhat like it would on your phone.

E.g.

<img src="example_html.png" />

# Credit

https://www.richinfante.com/2017/3/16/reverse-engineering-the-ios-backup/ contains a lot of info about the structure and contents of an iPhone backup. I wouldn't have been able to find the database files without it, and it saved me a lot of time not having to work out the tables and relations in the messages database.

# How to use this
I wrote it for myself with no expectation anyone else will use it, but if you want to:

### First, Back up your iPhone to your mac.
This might work on a PC, I didn't try it but theoretically the same files will be in the same places.

On my mac, it ended up in a folder under `~/Library/Application Support/MobileSync/Backup`.

Take a **copy** of the folder and work with the copy. `Application Support/MobileSync` has restricted permissions 
and this app may not be able to read data from that directory. Also you don't want to accidentally mess your backu up.

### Download and prepare `avifenc`

I downloaded it from the [AOMediaCodec/libavif/releases page](https://github.com/AOMediaCodec/libavif/releases).
Remember to follow the instructions in the readme to `sudo xattr -r -d com.apple.quarantine ./avifenc ./avifdec` otherwise it won't launch.

### Edit Program.cs to put the file paths in

Set `ImageConverter.AvifEncPath` to the path where you've downloaded and put `avifenc`. For example `"/Users/orion/Dev/avifenc-1.2.1/avifenc"`

Set `myName` to the name of the person's iPhone this is; It will be used for all messages sent by that person. E.g. `"Orion"`

Set `backupBasePath` to the place where you copied the backup to. E.g. `"/Users/orion/Downloads/MobileSyncBackup/00002020-000937B902000059"`

Set `outputDirectory` to the place where you'd like the generated HTML and media files to go. E.g. `$"/Users/orion/Downloads/Phone Message Exports/{myName}"`

### Compile and Run the program

You'll need the .NET SDK (I used .NET 9) and while I used JetBrains rider to build and execute the program a simple `dotnet run` on the command line should suffice.

## Image Conversion

If you have a lot of images, then converting them all to AVIF can take a long time. Be prepared for this.

**Q: Why convert images?**  
A: Because HEIC.

On modern iPhones, HEIC is the default image format. Unfortunately [only Safari on Mac/iOS can load HEIC images](https://caniuse.com/heif).
This utility produces webpages, and webpages full of images that the majority of web browsers can't load, are not very useful.

I wanted to convert them to something, and AVIF is a perfect choice. It [works across all modern browsers](https://caniuse.com/avif) and 
file sizes are **dramatically** smaller. For example a ~1MB HEIC file in JPEG format might be around 2MB. That same file in
AVIF will be around 300k. There is some small loss of quality, but we're backing up pictures that people sent and recieved
on their small mobile phone screens. At least in the testing I did, I couldn't notice a quality difference unless I was 
really zooming in and nitpicking.

Given how much more efficient AVIF is, I went and converted JPEG and PNG to AVIF as well. It's an amazing codec.

**Q: Why not convert videos?**  
A: We could likely recompress them into AV1 as well, and see similar dramatic file size reductions, but compressing
video takes **a lot** longer than images. It could take hours on a backup set with a reasonable number of videos, and
given that people typically send 50x more photos than videos it just wasn't worth it. 

# Disclaimers

### Why aren't there any tests? you're a professional developer aren't you?

I Initially wrote this in about one hour in the evening, with the help of ChatGPT, which got up to the point where it could 
extract messages to HTML. Plus another half hour to write this readme, tidy things up, and make it fit for publishing to GitHub. 
I know how to write tests and am a big advocate for them in my professional life, but I also have a family and limited time. For a one-shot HTML generator like this they aren't worth it.

### ChatGPT?

Note: I used ChatGPT to "get off the ground". It was very helpful in writing the initial HTML/CSS and SQL queries,
however I only used it for the initial version which didn't support attachments. Thereafter I wrote everything by hand.
I felt it was faster to simply write the code than to figure out which kind of magic prompt text to prompt the LLM with.
Attachments are complex, and honestly I'm not sure how I'd even go about prompting the LLM to extract them correctly.

I gave it this initial prompt:

```
I have a sqlite database file of imessage chats, according to this
---
[Here I inserted the description of the Messages table from Rich Infante's article]
---
Please write me a C# application using .NET 8 and whichever is the best SQLite library to extract all the conversations and render them into an HTML file. One HTML file for each conversation.
Include CSS so the messages look like they do on an iPhone
```

It wrote me a program in a rough form, which mostly worked but wasn't in the shape that I wanted. You can try it yourself!
I rearranged the code and then built the rest upon it. I wrote the addressbook stuff myself, it was easier than trying to explain to the LLM at that point.

I find that LLM's are a great way to really quickly bootstrap small utilities like this where the stakes are low. I don't care _at all_ about the HTML/CSS and I only care a tiny bit about the C# code structure. The app is tiny and I doubt I'll ever have to maintain it or explain to anyone else.

For professional code though, I find they're very poor. When working in codebases with an expected lifetime of 5+ years with a team, I find I pretty much know exactly the code I want to write; it needs to match the patterns in the codebase and be clear, easy to read and maintain, and we'll have to debug it in future. AI doesn't help much there at all. But hey, I'll take the win for this trivial app, it saved me several hours :-)