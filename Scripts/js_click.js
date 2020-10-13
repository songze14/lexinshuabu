function upstep() {

    var userid = $('#userid').val()
    var userpass = $('#userpass').val()
    var step = $('#step').val()
    $.ajax({
        url: "./up_step",
        type: "post",
        data: { 'usrid': userid, 'userpass': userpass, 'step': step },
        success: function (data) {
            var obj_data = $.parseJSON(data)
            if (obj_data.code == 200) {
                var step = obj_data.data.pedometerRecordHourlyList[0].step
                var arr_step = step.split(",")
                var all_step = eval(arr_step.join("+"))
                $(shuabu).html("今日最大步数为：" + Math.max(...arr_step));
                
            }
        }
    })


}