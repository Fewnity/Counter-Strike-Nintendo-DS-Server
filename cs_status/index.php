<!DOCTYPE html>
<html>

<head>
    <title>Status</title>
    <!-- CSS only -->
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-1BmE4kWBq78iYhFldvKuhfTAU6auU8tT94WrHftjDbrCEXSU1oBoqyl2QvZ6jIW3" crossorigin="anonymous">
</head>

<body>
<?php
function exception_error_handler($errno, $errstr, $errfile, $errline ) {
    throw new ErrorException($errstr, $errno, 0, $errfile, $errline);
}
set_error_handler("exception_error_handler");

$fp = NULL;

try {
    $fp = fsockopen("SERVER IP", 6003, $errno, $errstr, 3); //////////////////////// TO FILL ////////////////////////
}
catch(\Exception $ex) {
}
echo ('<div style="text-align: center;">');
if (!$fp) {
    echo ("Status : Offline");
} else {
    $out = "{STATUS}";
    fwrite($fp, $out);
    $result = "";
    while (!feof($fp)) {
        $result .= fgets($fp, 128);
    }
    fclose($fp);

    do
    {
        //Faster index of
        $StartIndex = -1;
        $EndIndex = -1;
        $len = strlen($result);

        for ($i2 = 0; $i2 < $len; $i2++)
        {
            if ($result[$i2] == '{')
            {
                $StartIndex = $i2;
                break;
            }
        }

        for ($i2 = $StartIndex; $i2 < $len; $i2++)
        {
            if ($result[$i2] == '}')
            {
                $EndIndex = $i2;
                break;
            }
        }

        $data = substr($result, $StartIndex + 1, $EndIndex - $StartIndex-1);
        $dataList = explode(";", $data);
        if($dataList[0] == "STATUS")
        {
            if($dataList[1] == "0"){
                echo "Status : Online";
            }else{
                echo "Status : Under maintenance";
            }
            echo "<br>";
            echo ("Connected players : " . $dataList[2] . " / " . $dataList[3]);
            
            $percent = $dataList[2] / $dataList[3] * 100;

            $color = "";
            if($percent >= 85){
                $color = "bg-warning";
            }else if($percent >= 70){
                $color = "bg-danger";
            }

            echo ('<div class="progress" style="margin:0px 5% 0px 5%;">');
            echo ('<div class="progress-bar progress-bar-striped ' . $color . '" role="progressbar" style="width: '.$percent.'%"></div>');
            echo ('</div>');

            echo ("Server version : " . $dataList[4]);
            echo "<br>";
            echo ("Game version : " . $dataList[5]);
        }
        
        $result = str_replace(substr($result, $StartIndex, $EndIndex - $StartIndex + 1), "", $result);
    }while (str_contains($result, '{') && str_contains($result, '}'));
}
echo "<br>";
    echo ('<a class="btn btn-primary" href="" role="button">Refresh</a>');
    echo ('</div>');
?>
</body>

</html>