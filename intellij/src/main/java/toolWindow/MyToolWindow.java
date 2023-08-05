package toolWindow;
import com.intellij.openapi.ui.LabeledComponent;
import com.intellij.openapi.ui.TextFieldWithBrowseButton;
import com.intellij.openapi.wm.ToolWindow;
import com.intellij.ui.components.JBPanel;
import com.intellij.ui.components.panels.VerticalLayout;
import services.GenerateClientHandler;
import services.KiotaGenerationLanguage;
import services.VersionHandler;
import javax.swing.*;
import java.awt.*;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.io.IOException;

public class MyToolWindow {
    private final JBPanel<JBPanel<?>> ParentPanel;
    VersionHandler versionHandler ;

    GenerateClientHandler generateClientHandler;
    public MyToolWindow(ToolWindow toolWindow) {
        ParentPanel = new JBPanel<>();
        versionHandler = new VersionHandler();
         generateClientHandler = new GenerateClientHandler();

    }
    public JComponent Addpanel() throws IOException {
        ParentPanel.setLayout(new BorderLayout());
        ParentPanel.add(getInput(), BorderLayout.CENTER);
        ParentPanel.add(getversion(), BorderLayout.SOUTH);
        return ParentPanel;
    }
    public JComponent getversion() throws IOException {
        JBPanel<JBPanel<?>> versionPanel = new JBPanel<>();
        versionPanel.setLayout(new BorderLayout());

        // Create a label to display kiota version
        JLabel versionLabel = new JLabel(versionHandler.getVersion());
        versionPanel.add(versionLabel, BorderLayout.CENTER);
        return versionPanel;
    }

    public JComponent getInput() {
        JBPanel<JBPanel<?>> mainPanel = new JBPanel<>();
        mainPanel.setLayout(new VerticalLayout(10));
        TextFieldWithBrowseButton yamlFilePathField = new TextFieldWithBrowseButton();
        TextFieldWithBrowseButton outputpath = new TextFieldWithBrowseButton();
        TextFieldWithBrowseButton clientClassField = new TextFieldWithBrowseButton();
        TextFieldWithBrowseButton postClientNameField = new TextFieldWithBrowseButton();
        mainPanel.setLayout(new VerticalLayout(10));

        LabeledComponent<TextFieldWithBrowseButton> labeledField1 = LabeledComponent.create(yamlFilePathField, "Enter the YAML file Path");
        LabeledComponent<TextFieldWithBrowseButton> labeledField4 = LabeledComponent.create(outputpath, "Enter the output directory");
        LabeledComponent<TextFieldWithBrowseButton> labeledField2 = LabeledComponent.create(clientClassField, "Enter the name of the client class");
        LabeledComponent<TextFieldWithBrowseButton> labeledField3 = LabeledComponent.create(postClientNameField, "Enter the name of the postclient to generate");
        mainPanel.add(labeledField1);
        mainPanel.add(labeledField4);
        mainPanel.add(labeledField2);
        mainPanel.add(labeledField3);

        KiotaGenerationLanguage language = KiotaGenerationLanguage.Java;
        String[] include = new String[0];
        String[] exclude = new String[0];

        // Add an action listener to the button to perform an action when clicked
        JButton executeButton = new JButton("Execute");
        executeButton.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                String descriptionPath = yamlFilePathField.getText();
                String output = outputpath.getText();
                String clientClass = clientClassField.getText();
                String clientClassNamespace = postClientNameField.getText();

                generateClientHandler.generateclient(descriptionPath, output, language, include, exclude, clientClass, clientClassNamespace);

                System.out.println(descriptionPath);
                System.out.println(output);
                System.out.println(language);
                System.out.println(clientClass);
                System.out.print(clientClassNamespace);

            }
        });
        mainPanel.add(executeButton);
        return mainPanel;
    }
}