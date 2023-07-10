package com.github.hossain2024.intellijtestplugin.toolWindow

import com.github.hossain2024.intellijtestplugin.MyBundle
import com.github.hossain2024.intellijtestplugin.services.MyProjectService
import com.intellij.openapi.components.service
import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.TextFieldWithBrowseButton
import com.intellij.openapi.wm.ToolWindow
import com.intellij.openapi.wm.ToolWindowFactory
import com.intellij.ui.JBColor
import com.intellij.ui.components.JBLabel
import com.intellij.ui.components.JBPanel
import com.intellij.ui.content.ContentFactory
import com.intellij.ui.layout.LCFlags
import java.awt.*
import javax.swing.*


class MyToolWindowFactory : ToolWindowFactory {

    init {
        thisLogger().warn("Don't forget to remove all non-needed sample code files with their corresponding registration entries in `plugin.xml`.")
    }

    override fun createToolWindowContent(project: Project, toolWindow: ToolWindow) {
        val myToolWindow = MyToolWindow(toolWindow)
        //val content = ContentFactory.getInstance().createContent(myToolWindow.getContent(), null, false)
        // val content2= ContentFactory.getInstance().createContent(myToolWindow.getContent2(), null, false)
        //val power_of_two = ContentFactory.getInstance().createContent(myToolWindow.getpowerof2(), null, false)
        //val input =  ContentFactory.getInstance().createContent(myToolWindow.getinput(), null, false)
        val input =  ContentFactory.getInstance().createContent(myToolWindow.Addpanel(), null, false)


       // toolWindow.contentManager.addContent(power_of_two)
        //toolWindow.contentManager.addContent(input)
        toolWindow.contentManager.addContent(input)


    }

    override fun shouldBeAvailable(project: Project) = true

    class MyToolWindow(toolWindow: ToolWindow) {

        private val service = toolWindow.project.service<MyProjectService>()
        val ParentPanel = JBPanel<JBPanel<*>>()


        fun Addpanel(): JComponent {
            ParentPanel.layout = BorderLayout()
            ParentPanel.add(getpowerof2(), BorderLayout.NORTH)
            return ParentPanel
        }

        /**
         * A function that returns a quote randomly.
         */
        fun getpowerof2(): JComponent {
            val mainPanel = JBPanel<JBPanel<*>>()
            mainPanel.layout = BorderLayout()

            // Prompt panel
            val promptPanel = JBPanel<JBPanel<*>>()
            val promptLabel = JBLabel(MyBundle.message("Label", ""))
            promptPanel.add(promptLabel)
            mainPanel.add(promptPanel, BorderLayout.NORTH)

            // Input panel
            val inputPanel = JBPanel<JBPanel<*>>()
            val textField = TextFieldWithBrowseButton()
            val resultLabel = JBLabel() // Declare resultLabel as a local variable
            val valueButton = JButton(MyBundle.message("value")).apply {
                addActionListener {
                    val inputNumber = textField.text.toIntOrNull()
                    if (inputNumber != null) {
                        val powerOfTwo = service.calculatePowerOfTwo(inputNumber)
                        resultLabel.text = MyBundle.message("resutmessage", powerOfTwo)
                    } else {
                        resultLabel.text = "Invalid input!"
                    }
                }
            }
            inputPanel.add(textField)
            inputPanel.add(valueButton)
            mainPanel.add(inputPanel, BorderLayout.CENTER)

            // Result panel
            val resultPanel = JBPanel<JBPanel<*>>()
            resultPanel.add(resultLabel)
            mainPanel.add(resultPanel, BorderLayout.SOUTH)

            return mainPanel;
        }

    }
}
