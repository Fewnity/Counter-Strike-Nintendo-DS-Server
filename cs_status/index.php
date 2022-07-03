<!DOCTYPE html>
<html>

<head>
    <title>Counter Strike Server Status</title>
    <!-- CSS only -->
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-1BmE4kWBq78iYhFldvKuhfTAU6auU8tT94WrHftjDbrCEXSU1oBoqyl2QvZ6jIW3" crossorigin="anonymous">
</head>

<body>
    <?php
    //Set error handler to avoid a PHP error at screen
    function exception_error_handler($errno, $errstr, $errfile, $errline)
    {
        throw new ErrorException($errstr, $errno, 0, $errfile, $errline);
    }
    set_error_handler("exception_error_handler");

    //Open a socket to the server
    $socket = NULL;
    try {
        $socket = fsockopen("SERVER IP", 6003, $errno, $errstr, 3); //////////////////////// TO FILL ////////////////////////
    } catch (\Exception $ex) {
    }

    //Print the server status
    echo ('<div style="text-align: center;">');
    if (!$socket) { //if the socket is not open, the server may be offline
        echo ("Status : Offline");
    } else {
        //Ask to the server for the status
        $out = "{43}";
        fwrite($socket, $out);

        //Read the result
        $result = "";
        while (!feof($socket)) {
            $result .= fgets($socket, 128);
        }
        //Close the socket
        fclose($socket);

        do {
            //Faster index of
            $StartIndex = -1;
            $EndIndex = -1;
            $len = strlen($result);

            for ($i2 = 0; $i2 < $len; $i2++) {
                if ($result[$i2] == '{') {
                    $StartIndex = $i2;
                    break;
                }
            }

            for ($i2 = $StartIndex; $i2 < $len; $i2++) {
                if ($result[$i2] == '}') {
                    $EndIndex = $i2;
                    break;
                }
            }
            //Remove the useless characters
            $data = substr($result, $StartIndex + 1, $EndIndex - $StartIndex - 1);
            //Split the data in an array
            $dataList = explode(";", $data);

            //Verify if the request is a STATUS request
            if ($dataList[0] == "STATUS") {
                //Check if the server is online or under maintenance
                if ($dataList[1] == "0") {
                    echo "Status : Online";
                } else {
                    echo "Status : Under maintenance";
                }

                //Print the number of connected players
                echo "<br>";
                echo ("Connected players : " . $dataList[2] . " / " . $dataList[3]);

                //Get the percentage of players connected compared to the maximum number of players
                $percent = $dataList[2] / $dataList[3] * 100;

                //Set the color of the bar according to the percentage
                $color = "";
                if ($percent >= 85) {
                    $color = "bg-warning";
                } else if ($percent >= 70) {
                    $color = "bg-danger";
                }

                //Print the bar
                echo ('<div class="progress" style="margin:0px 5% 0px 5%;">');
                echo ('<div class="progress-bar progress-bar-striped ' . $color . '" role="progressbar" style="width: ' . $percent . '%"></div>');
                echo ('</div>');

                //Print the server version
                echo ("Server version : " . $dataList[4]);

                //Print the lasted supported game version
                echo "<br>";
                echo ("Game version : " . $dataList[5]);
            }

            $result = str_replace(substr($result, $StartIndex, $EndIndex - $StartIndex + 1), "", $result);
        } while (str_contains($result, '{') && str_contains($result, '}'));
    }
    //Print a refresh button
    echo "<br>";
    echo ('<a class="btn btn-primary" href="" role="button">Refresh</a>');
    echo ('</div>');
    ?>
</body>

</html>