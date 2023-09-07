package toolWindow;
import com.intellij.openapi.project.Project;
import com.intellij.openapi.wm.ToolWindow;
import com.intellij.openapi.wm.ToolWindowFactory;
import com.intellij.ui.content.Content;
import com.intellij.ui.content.ContentFactory;
import org.jetbrains.annotations.NotNull;

import java.io.IOException;
public class Kiota implements ToolWindowFactory {
    MyToolWindow myToolWindow ;
    @Override
    public void createToolWindowContent(@NotNull Project project, @NotNull ToolWindow toolWindow) {
         myToolWindow = new MyToolWindow(toolWindow);
        ContentFactory contentFactory = ContentFactory.getInstance();
        Content content = null;
        try {
            content = contentFactory.createContent(myToolWindow.AddPanel(), null, false);
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
        toolWindow.getContentManager().addContent(content);

    }
}