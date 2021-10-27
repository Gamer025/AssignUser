using System;
using CommandLine;
using static CommandLine.Parser;
using AssignUser;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Octokit;

var parser = Default.ParseArguments<ActionInputs>(() => new(), args);
parser.WithNotParsed(
    errors =>
    {
        Console.Write(errors);
        Environment.Exit(2);
    });

parser.WithParsed(options => AssignUsers(options));

static void AssignUsers(ActionInputs inputs)
{
    List<string> users = new List<string>();
    List<string> nonAssignableUsers = new List<string>();

    //Split input and remove leading @
    if (inputs.separator is null)
    {
        users.Add(inputs.users.TrimStart('@'));
    }
    else
    {
        users.AddRange(inputs.users.Split(inputs.separator).Select(user => user.TrimStart('@')));
    }

    GitHubClient ghclient = new GitHubClient(new ProductHeaderValue("AssignUser"));
    ghclient.Credentials = new Credentials(inputs.token);

    users
    .Where(user => !ghclient.Issue.Assignee.CheckAssignee(inputs.Owner, inputs.Name, user).Result)
    .ToList()
    .ForEach(user =>
    {
        Console.WriteLine($"User {user} cannot be assigned, make sure he is member of a team.");
        users.Remove(user);
    }
    );


    Console.WriteLine($"Trying to assign all valid users: {String.Join(" ", users)}");
    ghclient.PullRequest.ReviewRequest.Create(inputs.Owner, inputs.Name, inputs.ID, new PullRequestReviewRequest(users, Array.Empty<string>()));

}