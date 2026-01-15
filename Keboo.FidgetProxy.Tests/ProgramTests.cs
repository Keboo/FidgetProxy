using System.CommandLine;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Keboo.FidgetProxy.Tests;

public class ProgramTests
{
    [Test]
    public async Task Invoke_WithHelpOption_DisplaysHelp()
    {
        using StringWriter stdOut = new();
        int exitCode = await Invoke("--help", stdOut);
        
        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(stdOut.ToString()).Contains("--help");
    }

    [Test]
    public async Task Invoke_StartCommand_ShowsInHelp()
    {
        using StringWriter stdOut = new();
        int exitCode = await Invoke("--help", stdOut);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(stdOut.ToString()).Contains("start");
        await Assert.That(stdOut.ToString()).Contains("stop");
        await Assert.That(stdOut.ToString()).Contains("clean");
    }

    private static Task<int> Invoke(string commandLine, StringWriter console)
    {
        RootCommand rootCommand = Program.BuildCommandLine();
        ParseResult parseResult = rootCommand.Parse(commandLine);
        parseResult.InvocationConfiguration.Output = console;
        return parseResult.InvokeAsync();
    }
}
