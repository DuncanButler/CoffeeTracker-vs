workspace "Name" "Description" {

    !identifiers hierarchical

    model {
        u = person "User"
        ss = softwareSystem "Software System" {
            wa = container "Web Application"
            be = container "API and Services"
            db = container "Database Schema" {
                tags "Database"
            }
        }

        u -> ss.wa "Uses"
        ss.wa -> ss.be "Calls"
        ss.be -> ss.db "Reads from and writes to"
    }

    views {
        systemContext ss "Context" {
            title "overall system context"
            include *
            autolayout lr
        }

        container ss "Container" {
            title "software system"
            include *
            autolayout lr
        }

        styles {
            element "Element" {
                color #ffffff
            }
            element "Person" {
                background #048c04
                shape person
            }
            element "Software System" {
                background #047804
            }
            element "Container" {
                background #55aa55
            }
            element "Database" {
                shape cylinder
            }
        }
    }

    configuration {
        scope softwaresystem
    }

}