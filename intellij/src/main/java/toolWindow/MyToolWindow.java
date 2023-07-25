package toolWindow;
import com.intellij.openapi.project.Project; // change this later
import com.intellij.openapi.ui.LabeledComponent;
import com.intellij.openapi.ui.TextFieldWithBrowseButton;
import com.intellij.openapi.wm.ToolWindow;
import com.intellij.ui.components.JBPanel;
import com.intellij.ui.components.panels.VerticalLayout;
import com.thetransactioncompany.jsonrpc2.JSONRPC2Request;
import com.thetransactioncompany.jsonrpc2.server.Dispatcher;
import services.HandlerDispatcher;
import services.kiotaVersionHandler;
import javax.swing.*;
import java.awt.*;

public class MyToolWindow {
   // private final MyKiotaProjectService service;
    private final JBPanel<JBPanel<?>> ParentPanel;
    HandlerDispatcher d = new HandlerDispatcher();

    public MyToolWindow(ToolWindow toolWindow) {
        Project project = toolWindow.getProject();
       // service = project.getService(MyKiotaProjectService.class);
        ParentPanel = new JBPanel<>();
    }

    public JComponent Addpanel() {
        ParentPanel.setLayout(new BorderLayout());
        ParentPanel.add(getInput(), BorderLayout.CENTER);
        ParentPanel.add(showversion(), BorderLayout.SOUTH);
        return ParentPanel;
    }

    public JComponent showversion() {
        JBPanel<JBPanel<?>> versionPanel = new JBPanel<>();
        versionPanel.setLayout(new BorderLayout());
        String method = "getkiotaversion";
        int ID = 0;
        JSONRPC2Request req= d.requestbuilder(method, ID);
        // Create a label to display the Kiota version
        JLabel versionLabel = new JLabel(d.getResp(req, (response)-> {
            return response.getResult().toString();
        }));
        versionPanel.add(versionLabel, BorderLayout.CENTER);

        return versionPanel;
    }

    public JComponent getInput() {
        JBPanel<JBPanel<?>> mainPanel = new JBPanel<>();
        mainPanel.setLayout(new VerticalLayout(10));

        LabeledComponent<TextFieldWithBrowseButton> labeledField1 = LabeledComponent.create(new TextFieldWithBrowseButton(), "Enter the YAML file Path");
        LabeledComponent<TextFieldWithBrowseButton> labeledField2 = LabeledComponent.create(new TextFieldWithBrowseButton(), "Enter the name of the client class");
        LabeledComponent<TextFieldWithBrowseButton> labeledField3 = LabeledComponent.create(new TextFieldWithBrowseButton(), "Enter the name of the postclient to generate");
        LabeledComponent<TextFieldWithBrowseButton> labeledField4 = LabeledComponent.create(new TextFieldWithBrowseButton(), "Enter the output directory");

        mainPanel.add(labeledField1);
        mainPanel.add(labeledField2);
        mainPanel.add(labeledField3);
        mainPanel.add(labeledField4);

        return mainPanel;
    }
}