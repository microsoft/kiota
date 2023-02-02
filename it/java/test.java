///usr/bin/env jbang "$0" "$@" ; exit $?

//DEPS com.microsoft.kiota:microsoft-kiota-abstractions:0.2.0
//DEPS com.microsoft.kiota:microsoft-kiota-serialization-json:0.2.0
//DEPS com.microsoft.kiota:microsoft-kiota-serialization-text:0.2.0
//DEPS com.microsoft.kiota:microsoft-kiota-serialization-form:0.2.0
//DEPS com.google.code.findbugs:jsr305:3.0.0
//SOURCES src/**/*.java

import static java.lang.System.*;

public class test {

    public static void main(String... args) {
        out.println("Everything compiles and classes are available in the compilation unit: " + new apisdk.models.Error());
    }
}
