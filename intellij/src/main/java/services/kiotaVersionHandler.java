package services;

import com.thetransactioncompany.jsonrpc2.*;

// The JSON-RPC 2.0 server framework package
import com.thetransactioncompany.jsonrpc2.server.*;


import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;

public class kiotaVersionHandler implements RequestHandler {

    @Override
    public String[] handledRequests() {
        return new String[]{"getkiotaversion"};
    }

    @Override
    public JSONRPC2Response process(JSONRPC2Request request, MessageContext requestCtx) {
        if (request.getMethod().equals("getkiotaversion")) {
            // Echo first parameter
            //ArrayList<String> response = new ArrayList<String>();

            String version = getkiotaversion();
            return new JSONRPC2Response(version, request.getID());
        } else {
            // Method name not supported
            return new JSONRPC2Response(JSONRPC2Error.METHOD_NOT_FOUND, request.getID());
        }
    }

    public String getkiotaversion() {
        String software = "kiota";

        try {
            Process process = new ProcessBuilder(software, "--version").start();

            // Read the command output
            BufferedReader reader = new BufferedReader(new InputStreamReader(process.getInputStream()));
            String line;
            StringBuilder output = new StringBuilder();

            while ((line = reader.readLine()) != null) {
                output.append(line).append("\n");
            }

            //process.waitfor- for the process to finish
            int exitCode = process.waitFor();

            if (exitCode == 0) {
                // Extract the version from the output
                String version = extractVersionFromOutput(output.toString());
                return version;
            } else {
                System.err.println("Command execution failed with exit code: " + exitCode);
            }
        } catch (IOException | InterruptedException e) {
            e.printStackTrace();
        }

        return "Unknown";
    }

    private String extractVersionFromOutput(String output) {
        String[] lines = output.trim().split("\n");
        if (lines.length > 0) {
            String version = lines[0]; //the version is present in the first line of code

            //need to remove additional info after + symbol
            int plusIndex = version.indexOf("+");
            if (plusIndex != -1) {
                return version.substring(0, plusIndex);
            } else {
                return version;
            }
        }
        return "-1";
    }
}











































/*import com.intellij.openapi.components.Service;
import com.intellij.openapi.diagnostic.Logger;
import com.intellij.openapi.project.Project;
import toolWindow.MyBundle;

import java.util.Random;

@Service(Service.Level.PROJECT)
public class MyKiotaProjectService {

    private final Project project;

    public MyKiotaProjectService(Project project) {
        this.project = project;
        Logger.getInstance(MyKiotaProjectService.class).info(MyBundle.message("projectService", project.getName()));
        Logger.getInstance(MyKiotaProjectService.class).warn("Don't forget to remove all non-needed sample code files with their corresponding registration entries in `plugin.xml`.");
    }


}*/
