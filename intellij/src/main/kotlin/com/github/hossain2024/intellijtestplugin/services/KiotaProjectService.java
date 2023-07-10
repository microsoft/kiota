package com.github.hossain2024.intellijtestplugin.services;

import com.intellij.openapi.components.Service;
import com.intellij.openapi.diagnostic.Logger;
import com.intellij.openapi.project.Project;
import com.github.hossain2024.intellijtestplugin.MyBundle;
import java.util.Random;

@Service(Service.Level.PROJECT)
public class  KiotaProjectService{
    private final Logger logger = Logger.getInstance(getClass());

    public   KiotaProjectService (Project project) {
        logger.info(MyBundle.message("projectService", project.getName()));
        logger.warn("Don't forget to remove all non-needed sample code files with their corresponding registration entries in `plugin.xml`.");
    }

    public int getRandomNumber() {
        return new Random().nextInt(100) + 1;
    }

    public long calculatePowerOfTwo(int exponent) {
        if (exponent < 0) {
            throw new IllegalArgumentException("Exponent must be non-negative");
        }
        return 1L << exponent;
    }
}



