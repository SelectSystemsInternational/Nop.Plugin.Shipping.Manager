/*
sendcloud service point add on code for NOP default Checkout/Shippingmethod.cshtml

Note: this only works if the shop is with
/Admin/Setting/Order > Disable "Order completed" page > UNCHECKED
For SEO purposes this is already required to get conversion data in the Google Analytics data
If "Order completed" page > CHECKED the Completed.cshtml step is skipped

Code below only works if the friendly names are set as follows:

PostNL                  =   PostNL Standard 0-23kg
PostNL avond            =   PostNL evening delivery + home address only 0-23kg
PostNL service point    =   PostNL service point 0-23kg

DHL                     =   DHLForYou Drop Off
DHL service point       =   DHL Parcel Connect 0-2kg to ParcelShop

bpost                   =   bpost (at)home 0-10kg
bpost service point     =   bpost (at)bpack 0-10kg

DPD                     =   DPD Home 0-31.5kg
DPD service point       =   DPD Pickup Point
*/

//used for checks if the user swithed back in the standard NOP checkout after selecting shipping method
var sessionSPid = sessionStorage.getItem("sessionservicepointid");
var sessionSPcarrier = sessionStorage.getItem("sessionservicepointcarrier");
var sessionSPaddress = sessionStorage.getItem("sessionservicepointaddress");
var sessionSPaddresshtml = sessionStorage.getItem("sessionservicepointaddresshtml");
var sessionSPponumber = sessionStorage.getItem("sessionservicepointponumber");
var sessionSPlat = sessionStorage.getItem("sessionservicepointlat");
var sessionSPlong = sessionStorage.getItem("sessionservicepointlong");

//current shipping address info for presets service point picker
var shippingcountryISOCode = '';
var shippingpostalCode = '';
var callGetShippingAddress = true;
var loadServicePointSelectionPage = true;
//mandatory elements in the ShippingMethod.cshtml view
var Sendcloudresultelement = document.getElementById('result'); //pre
var Sendcloudponumberelement = document.getElementById('ponumber'); //pre
//added dynamically
var changeSPaddress = "";
var postOfficeBox = "";

//added dynamically
//postnl
var PostNLtext = 'Delivery by PostNL during daytime';
var PostNLeveningtext = 'Delivery by PostNL in the evening';
var PostNLservicepointtext = 'Select a PostNL service point nearby and pick up your parcel after delivery';
//dhl
var DHLtext = 'Delivery by DHL during daytime';
var DHLservicepointtext = 'Select a DHL service point nearby and pick up your parcel after delivery';
//bpost
var Bposttext = 'Delivery by Bpost during daytime';
var Bpostservicepointtext = 'Select a Bpost service point nearby and pick up your parcel after delivery';
//dpd
var DPDtext = 'Delivery by DPD all around in Europe';
var DPDservicepointtext = 'Select a DPD service point nearby and pick up your parcel after delivery';

//variables for sendcloud service point api
var SSISMapiKey = null;
var SSISMcountry = null;
var SSISMpostalCode = null;
var SSISMlanguage = null;
var SSISMcarriers = null;

$(document).ready(function () {

  console.log("function GetShippingAddressData called");

  if (callGetShippingAddress) {

    console.log("retreiving shipping addres data");

    $.ajax({
      url: '/GetShippingAddress',
      async: false,
      data: {},
      dataType: "json",
      success: function (data) {
        SSISMapiKey = data.SSISMapiKey;
        shippingcountryISOCode = data.ShippingcountryISOCode;
        shippingpostalCode = data.ShippingpostalCode;
        changeSPaddress = data.ChangeSPaddress;
        postOfficeBox = data.PostOfficeBox
        loadServicePointSelectionPage = true
        if (shippingcountryISOCode != '')
          callGetShippingAddress = false;
      }
    });
  }

  //added dynamically
  var SSISMuseinfodivid = null;
  var SSISMuseaddressdivid = null;
  var changespaddresslink = null;

  //create extra optional explanation content elements to show below shipping method names
  //this way html is also possible if needed
  //postnl
  var divPostNL = $("<div id='PostNLextrainfo' class='SMextrainfo'>" + PostNLtext + "</div>");
  var divPostNLevening = $("<div id='PostNLeveningextrainfo' class='SMextrainfo'>" + PostNLeveningtext + "</div>");
  var divPostNLservicepoint = $("<div id='PostNLservicepoint' class='SMextrainfo'>" + PostNLservicepointtext + "</div>");
  //dhl
  var divDHL = $("<div id='DHLextrainfo' class='SMextrainfo'>" + DHLtext + "</div>");
  var divDHLservicepoint = $("<div id='DHLservicepoint' class='SMextrainfo'>" + DHLservicepointtext + "</div>");
  //bpost
  var divBpost = $("<div id='Bpostextrainfo' class='SMextrainfo'>" + Bposttext + "</div>");
  var divBpostservicepoint = $("<div id='Bpostservicepoint' class='SMextrainfo'>" + Bpostservicepointtext + "</div>");
  //dpd
  var divDPD = $("<div id='DPDextrainfo' class='SMextrainfo'>" + DPDtext + "</div>");
  var divDPDservicepoint = $("<div id='DPDservicepoint' class='SMextrainfo'>" + DPDservicepointtext + "</div>");

  //append the available content
  //postnl
  $(':radio[value="PostNL___Shipping.SendCloud"]').parent().append(divPostNL);
  $(':radio[value="PostNL avond___Shipping.SendCloud"]').parent().append(divPostNLevening);
  $(':radio[value="PostNL service point___Shipping.SendCloud"]').parent().append(divPostNLservicepoint);
  //dhl
  $(':radio[value="DHL___Shipping.SendCloud"]').parent().append(divDHL);
  $(':radio[value="DHL service point___Shipping.SendCloud"]').parent().append(divDHLservicepoint);
  //bpost
  $(':radio[value="bpost___Shipping.SendCloud"]').parent().append(divBpost);
  $(':radio[value="bpost service point___Shipping.SendCloud"]').parent().append(divBpostservicepoint);
  //dpd
  $(':radio[value="DPD___Shipping.SendCloud"]').parent().append(divDPD);
  $(':radio[value="DPD service point___Shipping.SendCloud"]').parent().append(divDPDservicepoint);

  //create empty elements for displaying the selected servicepoint address afterwards per service point shipping method
  var $divPostNLDisplaySPaddress = $("<div id='PostNLservicepointaddress' class='SMspaddress'></div>");
  var $divDHLDisplaySPaddress = $("<div id='DHLservicepointaddress' class='SMspaddress'></div>");
  var $divDPDDisplaySPaddress = $("<div id='DPDservicepointaddress' class='SMspaddress'></div>");
  var $divBpostDisplaySPaddress = $("<div id='Bpostservicepointaddress' class='SMspaddress'></div>");

  //append the empty elements for the selected service point address
  $(':radio[value="PostNL service point___Shipping.SendCloud"]').parent().append($divPostNLDisplaySPaddress);
  $(':radio[value="DHL service point___Shipping.SendCloud"]').parent().append($divDHLDisplaySPaddress);
  $(':radio[value="DPD service point___Shipping.SendCloud"]').parent().append($divDPDDisplaySPaddress);
  $(':radio[value="bpost service point___Shipping.SendCloud"]').parent().append($divBpostDisplaySPaddress);

  if (typeof sessionSPid === 'undefined' || sessionSPid === null) {

    console.log('no servicepoint data in session');
    /*
    prevent a service point shipping method on top of the list
    by setting it with a higher orderby value than a non servicepoint method
    In this case the radio is already selected but not triggered
    line below can also prevent it:
    */
    //$("input[type=radio][name=shippingoption]:first").attr('checked', false);

  }
  else {

    console.log('session data present');

    var usecarrier = sessionStorage.getItem("sessionservicepointcarrier");
    var useaddresshtml = sessionStorage.getItem("sessionservicepointaddresshtml");

    //console.log('session data carrier: ' + usecarrier );

    switch (usecarrier) {
      case 'postnl':
        SSISMuseinfodivid = 'PostNLservicepoint';
        SSISMuseaddressdivid = 'PostNLservicepointaddress';
        document.getElementById(SSISMuseinfodivid).innerHTML = "";
        document.getElementById(SSISMuseaddressdivid).innerHTML = useaddresshtml;
        break;
      case 'dhl':
        SSISMuseinfodivid = 'DHLservicepoint';
        SSISMuseaddressdivid = 'DHLservicepointaddress';
        document.getElementById(SSISMuseinfodivid).innerHTML = "";
        document.getElementById(SSISMuseaddressdivid).innerHTML = useaddresshtml;
        break;
      case 'dpd':
        SSISMuseinfodivid = 'DPDservicepoint';
        SSISMuseaddressdivid = 'DPDservicepointaddress';
        document.getElementById(SSISMuseinfodivid).innerHTML = "";
        document.getElementById(SSISMuseaddressdivid).innerHTML = useaddresshtml;
        break;
      case 'bpost':
        SSISMuseinfodivid = 'Bpostservicepoint';
        SSISMuseaddressdivid = 'Bpostservicepointaddress';
        document.getElementById(SSISMuseinfodivid).innerHTML = "";
        document.getElementById(SSISMuseaddressdivid).innerHTML = useaddresshtml;
        break;
      default:
      //do nothing
    }
  }

  //function to detect which method is selected
  $('input[type=radio][name=shippingoption]').change(function () {

    if (loadServicePointSelectionPage) {

      loadServicePointSelectionPage = false;

      //empty and hide elements
      ResetSPdata();

      console.log("SSISMapiKey: " + SSISMapiKey);
      console.log("shippingcountryISOCode: " + shippingcountryISOCode);
      console.log("shippingpostalCode: " + shippingpostalCode);
      console.log("changeSPaddress: " + changeSPaddress);
      console.log("postOfficeBox: " + postOfficeBox);
      console.log("loadServicePointSelectionPage: " + loadServicePointSelectionPage);
      console.log("callGetShippingAddress: " + callGetShippingAddress);

      //reset session items
      ResetSessionData();

      //apply the deliveryaddress data
      SSISMcountry = shippingcountryISOCode;
      SSISMpostalCode = shippingpostalCode;
      SSISMlanguage = shippingcountryISOCode;

      //so we deduce the carrier from the friendly name label
      var shippingmethodname = $(this).parent().find(".ng-binding").text().toLowerCase();
      var showservicepointsfor = ''

      if (shippingmethodname.search("postnl") > -1) {
        if (shippingmethodname.search("service point") > -1) {
          showservicepointsfor = 'postnl';
        }
      }
      else if (shippingmethodname.search("dhl") > -1) {
        if (shippingmethodname.search("service point") > -1) {
          showservicepointsfor = 'dhl';
        }
      }
      else if (shippingmethodname.search("dpd") > -1) {
        if (shippingmethodname.search("service point") > -1) {
          showservicepointsfor = 'dpd';
        }
      }
      else if (shippingmethodname.search("bpost") > -1) {
        if (shippingmethodname.search("service point") > -1) {
          showservicepointsfor = 'bpost';
        }
      }

      console.log('show service points for: ' + showservicepointsfor);

      if (showservicepointsfor == 'postnl') {
        SSISMcarriers = 'postnl';
        openServicePointPicker(SSISMapiKey, SSISMcountry, SSISMlanguage, SSISMpostalCode, SSISMcarriers);
      }
      else if (showservicepointsfor == 'dhl') {
        SSISMcarriers = 'dhl';
        openServicePointPicker(SSISMapiKey, SSISMcountry, SSISMlanguage, SSISMpostalCode, SSISMcarriers);
      }
      else if (showservicepointsfor == 'dpd') {
        SSISMcarriers = 'dpd';
        openServicePointPicker(SSISMapiKey, SSISMcountry, SSISMlanguage, SSISMpostalCode, SSISMcarriers);
      }
      else if (showservicepointsfor == 'bpost') {
        SSISMcarriers = 'bpost';
        openServicePointPicker(SSISMapiKey, SSISMcountry, SSISMlanguage, SSISMpostalCode, SSISMcarriers);
      }
      else {
        //remove service point session data
        ResetSessionData()
      }
    }
  });
});

//when the user selected the wrong servicepoint
function ChangeSPaddress() {
  $('input[type=radio][name=shippingoption]:checked').trigger("change");
}

//used for resetting and start over with service point items
function ResetSPdata() {

  $('#result').empty();
  $('#ROPCServicePointAddress').empty();
  document.getElementById('SMROPCspsection').style.display = "none";

  if ($('#PostNLservicepoint').length) {
    $('#PostNLservicepoint').html(PostNLservicepointtext);
  }

  if ($('#Bpostservicepoint').length) {
    $('#Bpostservicepoint').html(Bpostservicepointtext);
  }

  if ($('#DHLservicepoint').length) {
    $('#DHLservicepoint').html(DHLservicepointtext);
  }

  if ($('#DPDservicepoint').length) {
    $('#DPDservicepoint').html(DPDservicepointtext);
  }
}

//used to reset the session variables
function ResetSessionData() {
  sessionStorage.removeItem("sessionservicepointid");
  sessionStorage.removeItem("sessionservicepointcarrier");
  sessionStorage.removeItem("sessionservicepointaddress");
  sessionStorage.removeItem("sessionservicepointaddresshtml");
  sessionStorage.removeItem("sessionservicepointponumber");
  sessionStorage.removeItem("sessionservicepointlat");
  sessionStorage.removeItem("sessionservicepointlong");
  loadServicePointSelectionPage = true;
  callGetShippingAddress = true;
  console.log('session data removed');
}

function openServicePointPicker(apiKey, country, language, postalCode, carriers, servicePointId, postNumber, servicePointId, postNumber) {

  var config = {
    apiKey: apiKey,
    country: country,
    postalCode: postalCode,
    language: language,
    carriers: carriers,
    servicePointId: servicePointId,
    postNumber: postNumber
  }

  sendcloud.servicePoints.open(
    config,
    function (servicePointObject, postNumber) {
      if ($.isEmptyObject(servicePointObject)) {
        //no data received from the servicepoint selector
        //display a message to try again or select other shipping method
      }
      else {

        //put the received data in our selected element
        Sendcloudresultelement.innerHTML = JSON.stringify(servicePointObject, null, 2);
        var spponumber = '';
        Sendcloudponumberelement.innerHTML = postNumber;

        //for testing only to display the full results:
        //Sendcloudresultelement.style.display = 'block';
        //Sendcloudponumberelement.style.display = 'block';

        //save the ponumber value
        spponumber = postNumber;

        //get the servicepoint id
        var obj = JSON.parse(Sendcloudresultelement.innerHTML);
        var spidreceived = obj.id;

        //get the service point carrier value
        var spcarrierreceived = obj.carrier;

        //display the selected servicepoint address to the customer:
        var spname = obj.name;
        var spstreet = obj.street;
        var sphouse_number = obj.house_number;
        var sppostal_code = obj.postal_code;
        var spcity = obj.city;
        var spcountry = obj.country;
        var splat = obj.latitude;
        var splong = obj.longitude;

        //create a html formatted address and add the change address html link code
        var spaddressreceivedhtml = spname + '<br />' + spstreet + ' ' + sphouse_number + '<br />' + sppostal_code + ' ' + spcity + ' (' + spcountry + ')';

        if (spponumber != '') {
          spponumber = spponumber.replace(/ /g, '');
          spaddressreceivedhtml += '<br />' + postOfficeBox + ": " + spponumber;
        }

        changespaddresslink = '<b><a href="#" class="button-2 estimate-shipping-button" onclick="ChangeSPaddress()">' + changeSPaddress + '</a>' + '</b>';
        spaddressreceivedhtml += '<br /><br />' + changespaddresslink

        //create a non html formatted servicepoint address for useage in alerts etc
        var spaddressreceived = spname + '\n' + spstreet + ' ' + sphouse_number + '\n' + sppostal_code + ' ' + spcity + ' (' + spcountry + ')';

        //set the address visible for the user
        document.getElementById('ROPCServicePointAddress').innerHTML = spaddressreceivedhtml;
        document.getElementById('SMROPCspsection').style.display = "block";

        //set the session items
        sessionStorage.setItem("sessionservicepointid", spidreceived);
        sessionStorage.setItem("sessionservicepointcarrier", spcarrierreceived);
        sessionStorage.setItem("sessionservicepointaddress", spaddressreceived);
        sessionStorage.setItem("sessionservicepointaddresshtml", spaddressreceivedhtml);
        sessionStorage.setItem("sessionservicepointponumber", spponumber);
        sessionStorage.setItem("sessionservicepointlat", splat);
        sessionStorage.setItem("sessionservicepointlong", splong);

        loadServicePointSelectionPage = true;
        callGetShippingAddress = true;

      }
    },
    function (errors) {
      // Servier point selection canceled
      ResetSessionData()
      // Select first radio button
      radiobtn = document.getElementById("shippingoption_0");
      radiobtn.checked = true;
      errors.forEach(function (error) {
        console.log('Failure callback, reason: ' + error);
      })
    }
  )
}