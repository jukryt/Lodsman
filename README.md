<h1 align="center">Lodsman</h1>

A utility for monitoring the network activity of Windows processes and using the obtained addresses to configure routing.

## Command-line arguments
<table>
    <thead>
        <tr>
            <th>root</th>
            <th>argument</th>
            <th>value</th>
            <th>mandatory</th>
            <th>multiple</th>
            <th>description</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td rowspan=4><kbd>/keen</kbd> or <kbd>/keenetic</kbd></td>
            <td><kbd>-a</kbd> or <kbd>--address</kbd></td>
            <td>address</td>
            <td>required</td>
            <td>only one</td>
            <td>keenetic(netcraze) ip address</td>
        </tr>
        <tr>
            <td><kbd>-u</kbd> or <kbd>--user</kbd></td>
            <td>name</td>
            <td>required</td>
            <td>only one</td>
            <td>keenetic(netcraze) user name</td>
        </tr>
        <tr>
            <td><kbd>-p</kbd> or <kbd>--password</kbd></td>
            <td>password</td>
            <td>required</td>
            <td>only one</td>
            <td>keenetic(netcraze) user password</td>
        </tr>
        <tr>
            <td><kbd>-ln</kbd> or <kbd>--list-name</kbd></td>
            <td>dns route list name</td>
            <td>required</td>
            <td>only one</td>
            <td>keenetic(netcraze) dns route list name</td>
        </tr>
        <tr>
            <td rowspan=5></td>
            <td><kbd>-pn</kbd> or <kbd>--process-name</kbd></td>
            <td>process name</td>
            <td>required</td>
            <td>one or more</td>
            <td>windows process name</td>
        </tr>
        <tr>
            <td><kbd>-cbe</kbd> or <kbd>--clear-before-exit</kbd></td>
            <td></td>
            <td>optional</td>
            <td>only one</td>
            <td>if there are any, the dns route list will be cleared before the process ends</td>
        </tr>
        <tr>
            <td><kbd>-is</kbd> or <kbd>--install-service</kbd></td>
            <td></td>
            <td>optional</td>
            <td>only one</td>
            <td>install and run utility as service</td>
        </tr>
        <tr>
            <td><kbd>-us</kbd> or <kbd>--uninstall-service</kbd></td>
            <td></td>
            <td>optional</td>
            <td>only one</td>
            <td>stop and uninstall service</td>
        </tr>
        <tr>
            <td><kbd>-?</kbd> or <kbd>-h</kbd> or <kbd>--help</kbd></td>
            <td></td>
            <td>optional</td>
            <td>only one</td>
            <td>show help</td>
        </tr>
    </tbody>
</table>

## Usage
Monitor `process_name_1` and `process_name_2` and send all IP addresses they accessed to `list_name` on the keenetic(netcraze) router:
```
Lodsman.exe /keen -a 192.168.1.1 -u <user> -p <password> -ln <list_name> -pn <process_name_1> -pn <process_name_2>
```
Install and run service for monitor `process_name` and send all IP addresses they accessed to `list_name` on the keenetic(netcraze) router:
```
Lodsman.exe /keen -a 192.168.1.1 -u <user> -p <password> -ln <list_name> -pn <process_name> -is
```

## Log for service
If the utility is running as a service, standard output will be redirected to the file: `%ProgramData%\Lodsman\<service_name>.log`.

## Additional information on using the Keenetic (Netcraze) router
Before running, the utility reads all existing addresses from the specified route list. If the list contains IP subnets, then IP addresses within those subnets will not be added.
