<h1 align="center">Lodsman</h1>

A utility for monitoring the network activity of Windows processes and using the obtained addresses to configure routing.

## Command-line arguments
| oprion | argument | mandatory | possible number | description |
| --- | --- | --- | --- | --- |
| -ka, --keen-address | address | required | only one | keenetic(netcraze) router ip address |
| -ku, --keen-user | name | required | only one | keenetic(netcraze) router user name |
| -kp, --keen-password | password | required | only one | keenetic(netcraze) router user password |
| -kln, --keen-list-name | dns route list name | required | only one | keenetic(netcraze) router dns route list name |
| -pn, -process-name | process name | required | one or more | windows process name |
| -cbe, -clear-before-exit | - | optional | only one | if there are any, the dns route list will be cleared before the process ends |
| -?, -h, --help | - | optional | only one | show help |

## Usage
Monitor **process_name_1** and **process_name_2** and send all IP addresses they accessed to **list_name** on the router.
```
Lodsman.exe -ka 192.168.1.1 -ku <user> -kp <password> -kln <list_name> -pn <process_name_1> -pn <process_name_2>
```

## Additional information on using the Keenetic (Netcraze) router
Before running, the utility reads all existing addresses from the specified route list. If the list contains IP subnets, then IP addresses within those subnets will not be added.
