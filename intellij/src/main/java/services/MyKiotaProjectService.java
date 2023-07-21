package services;

import com.intellij.openapi.components.Service;
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

    public int getRandomNumber() {
        return new Random().nextInt(100) + 1;
    }
}
