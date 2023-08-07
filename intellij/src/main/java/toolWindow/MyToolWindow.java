package toolWindow;
import com.intellij.openapi.ui.ComboBox;
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
import java.io.File;
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

    /**
     * This method displays version of kiota.
     * @return versionpanel
     * @throws IOException
     */
    public JComponent getversion() throws IOException {
        JBPanel<JBPanel<?>> versionPanel = new JBPanel<>();
        versionPanel.setLayout(new BorderLayout());

        // Create a label to display kiota version
        JLabel versionLabel = new JLabel(versionHandler.getVersion());
        versionPanel.add(versionLabel, BorderLayout.CENTER);
        return versionPanel;
    }

    /**
     * this method sets the client(UI) to get input from user and generate clientclasses.
     * @return panel
     */
    public JComponent getInput() {
        JBPanel<JBPanel<?>> mainPanel = new JBPanel<>();
        mainPanel.setLayout(new VerticalLayout(10));

        // Create the TextFieldWithBrowseButton components
        TextFieldWithBrowseButton yamlFilePathField = new TextFieldWithBrowseButton();
        TextFieldWithBrowseButton outputpath = new TextFieldWithBrowseButton();
        TextFieldWithBrowseButton clientClassField = new TextFieldWithBrowseButton();
        TextFieldWithBrowseButton postClientNameField = new TextFieldWithBrowseButton();

        // Input labels
        LabeledComponent<TextFieldWithBrowseButton> yamlFilePath_label = LabeledComponent.create(yamlFilePathField, "Enter the YAML file Path");
        LabeledComponent<TextFieldWithBrowseButton> outputpath_Label = LabeledComponent.create(outputpath, "Enter the output directory");
        JLabel languageLabel = new JLabel("Select a Language");
        LabeledComponent<TextFieldWithBrowseButton> clientclassname_label = LabeledComponent.create(clientClassField, "Enter a name for the client class");
        LabeledComponent<TextFieldWithBrowseButton> clientclassnameSpace_label = LabeledComponent.create(postClientNameField, "Enter the name of the client class namespace");

        JComboBox<KiotaGenerationLanguage> languageComboBox = new ComboBox<>(KiotaGenerationLanguage.values());
        String[] include = new String[0]; //empty string
        String[] exclude = new String[0]; //empty String

        // Add an action listener to the  genrate button to perform an action when clicked
        JButton generateButton = new JButton("Generate");
        generateButton.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                String descriptionPath = yamlFilePathField.getText();
                String output = outputpath.getText();
                String clientClass = clientClassField.getText();
                String clientClassNamespace = postClientNameField.getText();
                KiotaGenerationLanguage selectedLanguage = (KiotaGenerationLanguage) languageComboBox.getSelectedItem();
                generateClientHandler.generateclient(descriptionPath, output, selectedLanguage, include, exclude, clientClass, clientClassNamespace);
            }
        });

        // Add the ActionListener to the  (yamlFilePathField) browse icon
        yamlFilePathField.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                JFileChooser fileChooser = new JFileChooser();
                fileChooser.setFileSelectionMode(JFileChooser.FILES_AND_DIRECTORIES);
                int returnValue = fileChooser.showOpenDialog(null);

                if (returnValue == JFileChooser.APPROVE_OPTION) {
                    File selectedFile = fileChooser.getSelectedFile();
                    String selectedPath = selectedFile.getAbsolutePath();
                    yamlFilePathField.setText(selectedPath);
                }
            }
        });
        // Add the ActionListener to the  (outputpath) browse icon
        outputpath.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                JFileChooser fileChooser = new JFileChooser();
                fileChooser.setFileSelectionMode(JFileChooser.FILES_AND_DIRECTORIES);
                int returnValue = fileChooser.showOpenDialog(null);

                if (returnValue == JFileChooser.APPROVE_OPTION) {
                    File selectedFile = fileChooser.getSelectedFile();
                    String selectedPath = selectedFile.getAbsolutePath();
                    outputpath.setText(selectedPath);
                }
            }
        });

        mainPanel.add(yamlFilePath_label);
        mainPanel.add(outputpath_Label);
        mainPanel.add(languageLabel);
        mainPanel.add(languageComboBox);
        mainPanel.add(clientclassname_label);
        mainPanel.add(clientclassnameSpace_label);
        mainPanel.add(generateButton);
        return mainPanel;
    }
}

