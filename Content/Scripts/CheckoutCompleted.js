/*
Sendcloud service point 
Add on code for NOP default Checkout/Completed.cshtml
keep in mind: this only works if the shop is with
/Admin/Setting/Order > Disable "Order completed" page > UNCHECKED
For SEO purposes this is already required to get conversion data in the Google Analytics data
If "Order completed" page > CHECKED the Completed.cshtml step is skipped in the checkout
*/

$(document).ready(function () {
  //alert("section completed");
  //check if sp data is present
  var sessionSPid = sessionStorage.getItem("sessionservicepointid");
  var sessionSPcarrier = sessionStorage.getItem("sessionservicepointcarrier");
  var sessionSPaddress = sessionStorage.getItem("sessionservicepointaddress");
  var sessionSPlat = sessionStorage.getItem("sessionservicepointlat");
  var sessionSPlong = sessionStorage.getItem("sessionservicepointlong");
  var sessionSPponumber = sessionStorage.getItem("sessionservicepointponumber");

  if (typeof sessionSPid === 'undefined' || sessionSPid === null) {
    console.log("no spdata in session");
  }
  else {

    console.log('service point id from shipping method:\n' + sessionSPid);
    console.log('service point carrier from shipping method:\n' + sessionSPcarrier);
    console.log('service point address from shipping method:\n' + sessionSPaddress);
    console.log('service point po number from shipping method:\n' + sessionSPponumber);
    console.log('service point geodata from shipping method\n\nlat: ' + sessionSPlat + '\nlong: ' + sessionSPlong);

    //servicepoint names can be the same so not fail safe for cross checks
    //use the service point id, carrier, latitude and longitude for cross checks
    var sporder = document.getElementById('customOrderNumber').value;
    var checksum = document.getElementById('checksum').value;

    var spuseurl = "/Sendcloud/SaveServicePoint?";
    var data = "orderId=" + sporder;
    data += "&spid=" + sessionSPid;
    data += "&spcarrier=" + sessionSPcarrier;
    data += "&spaddress=" + encodeURIComponent(sessionSPaddress);
    data += "&splat=" + sessionSPlat;
    data += "&splong=" + sessionSPlong;
    data += "&spponumber=" + sessionSPponumber;
    data += "&checksum=" + encodeURIComponent(checksum);

    //console.log(spuseurl);
    PostSPdata(spuseurl, data);
  }
});

function PostSPdata(spuseurl, data) {
  if (spuseurl !== null) {
    var xhr = new XMLHttpRequest();
    xhr.open("POST", spuseurl);
    xhr.setRequestHeader('Content-type', 'application/x-www-form-urlencoded');
    xhr.done = function () {
      if (xhr.readyState === 4 && xhr.status === 200) {
        //when the data is conected to the order, the session data can be removed for this checkout
        sessionStorage.removeItem("sessionservicepointid");
        sessionStorage.removeItem("sessionservicepointcarrier");
        sessionStorage.removeItem("sessionservicepointaddress");
        sessionStorage.removeItem("sessionservicepointponumber");        
        sessionStorage.removeItem("sessionservicepointaddresshtml");
        sessionStorage.removeItem("sessionservicepointlat");
        sessionStorage.removeItem("sessionservicepointlong");

        console.log("session data sent with success");
      }
    };
    xhr.send(data);
  }
}
