# DevGPT - AI development project editor

# Initialization

1. Create an appsettings.json file with the following contents:
{
  "OpenAI": {
    "ApiKey": "Your API key here"
  }
}
2. Replace the 'Your API key here' with your OpenAI API key.
3. Ensure you have .NET SDK installed on your system.
4. Start the application by running `dotnet run` in your terminal within the project directory.

Only works on Windows right now.

# Usage

1. Start the application.
2. Replace the folder path with the path to your development project.
3. Check on embeddings if you want to generate new embeddings and supply a path where it needs to write its embeddings file. This should be on except if you have already generated embeddings and you want to use those. For instance, if you only asked a question.
4. Check on history if you want to include history about what you are doing. For instance, if you want to ask multiple questions in a row about the same subject.
5. Provide an instruction on what you would like to have updated in the project.
6. Press 'Update code' or 'Ask a question' depending on what you would like to do.
7. The application will now update the code in the project and come back with a message.