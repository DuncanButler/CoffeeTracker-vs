workspace "Name" "Description" {

    !identifiers hierarchical

    model {
        u = person "User"
        ss = softwareSystem "Software System" {
            wa = container "Web Application"
            be = container "Backend and API" {
                api = component "API and Services"
                dta = component "Data Context"
                mig = component "Migrations"
                mod = component "Models"
            }
            db = container "Weather Db" {
                tags "Database"

                ft = component "Forcast Table"
            }
        }

        u -> ss.wa "Uses"
        ss.wa -> ss.be.api "Calls"
        ss.be.api -> ss.be.dta "Used to get Data"
        ss.be.api -> ss.be.mod "Uses"
        ss.be.mig -> ss.be.dta "Used to get Data"
        ss.be.mig -> ss.be.mod "Uses"
        
        ss.be.dta -> ss.db.ft "Reads from and writes to"
    }

    views {
        systemContext ss "Context" {
            title "Overall System Context"
            include *
            autolayout lr
        }

        container ss "Container" {
            title "Software System"
            include *
            autolayout lr
        }

        component ss.be "Component" {
            title "Backend Services"     
            include *
        }

        component ss.db "DB-Component" {
            title "Weather Database"
            include *
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