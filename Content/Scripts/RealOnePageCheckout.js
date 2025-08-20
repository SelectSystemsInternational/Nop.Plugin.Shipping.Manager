/*
Sendcloud service point 
add on code for RealOnePageCheckout.cshtml
*/

//current shipping address info for presets service point picker
//hard coded for now untill this can be provided by a controller
var shippingcountryISOCode = '';
var shippingpostalCode = '';
var callGetShippingAddress = true;
var loadServicePointSelectionPage = true;

//mandatory elements in the ShippingMethod.cshtml view
var Sendcloudresultelement = document.getElementById('result'); //pre
var Sendcloudponumberelement = document.getElementById('ponumber'); //pre
//added dynamically
var changeSPaddress = ""; //localized value is picked up with GetShippingAddress
var postOfficeBox = ""; //localized value is picked up with GetShippingAddress
var changespaddresslink = "";

//variables for sendcloud service point api
var SSISMapiKey = '';
var SSISMcountry = null;
var SSISMpostalCode = null;
var SSISMlanguage = null;
var SSISMcarriers = null;

$(document).ready(function () {
  console.log("document ready");
  GetShippingAddressData();
});

$(document).ajaxStop(function () {
  console.log("ROPC ajaxStop");
  GetShippingAddressData();
});

function GetShippingAddressData() {

  console.log("function GetShippingAddressData called");

  if (callGetShippingAddress) {

    console.log("retreiving shipping addres data");

    $.ajax({
      url: '/GetShippingAddress',
      async: true,
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

    console.log("GetShippingAddress > SSISMapiKey: " + SSISMapiKey);
    console.log("GetShippingAddress > shippingcountryISOCode: " + shippingcountryISOCode);
    console.log("GetShippingAddress > shippingpostalCode: " + shippingpostalCode);
    console.log("GetShippingAddress > changeSPaddress: " + changeSPaddress);
    console.log("GetShippingAddress > postOfficeBox: " + postOfficeBox);
    console.log("GetShippingAddress > loadServicePointSelectionPage: " + loadServicePointSelectionPage);
    console.log("GetShippingAddress > callGetShippingAddress: " + callGetShippingAddress);

  }

}

//function to detect which method is selected
$('#RPOCshippingmethoddiv').on('change', 'input[type=radio][name=shippingmethod]', function () {

  if (loadServicePointSelectionPage) {

    loadServicePointSelectionPage = false;

    //reset previous values
    ResetSPdata();

    console.log("SSISMapiKey: " + SSISMapiKey);
    console.log("shippingcountryISOCode: " + shippingcountryISOCode);
    console.log("shippingpostalCode: " + shippingpostalCode);
    console.log("changeSPaddress: " + changeSPaddress);
    console.log("postOfficeBox: " + postOfficeBox);
    console.log("loadServicePointSelectionPage: " + loadServicePointSelectionPage);
    console.log("callGetShippingAddress: " + callGetShippingAddress);

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

//when the last order was placed and a service point was used, the servicepoint radio is selected but no service point data is set on load.
//can the shipping method checked function be disabled so a user always has to select a shipping method?

$('#ROPCbillingaddressdiv').on('change', function () {

  loadServicePointSelectionPage = true;
  ResetSPdata();

  var element = document.getElementById("billingCountry");
  var bc = element.options[element.selectedIndex].text;
  var zip = document.getElementById("billingZipPostalCode").value;
  $.ajax({
    url: '/GetShippingAddress',
    async: true,
    data: {
      "billingCountry": bc,
      "zipCode": zip
    },
    dataType: "json",
    global: false,
    success: function (data) {
      SSISMapiKey = data.SSISMapiKey;
      shippingcountryISOCode = data.ShippingcountryISOCode;
      shippingpostalCode = data.ShippingpostalCode;
      changeSPaddress = data.ChangeSPaddress;
      postOfficeBox = data.PostOfficeBox;
      loadServicePointSelectionPage = true;
      if (shippingcountryISOCode != '')
        callGetShippingAddress = false;
    }
  });
});

$('#ROPCshippingaddressdiv').on('change', function () {

  loadServicePointSelectionPage = true;
  ResetSPdata();

  var element = document.getElementById("shippingCountry");
  var bc = element.options[element.selectedIndex].text;
  var zip = document.getElementById("shippingZipPostalCode").value;
  $.ajax({
    url: '/GetShippingAddress',
    async: true,
    data: {
      "billingCountry": bc,
      "zipCode": zip
    },
    dataType: "json",
    global: false,
    success: function (data) {
      SSISMapiKey = data.SSISMapiKey;
      shippingcountryISOCode = data.ShippingcountryISOCode;
      shippingpostalCode = data.ShippingpostalCode;
      changeSPaddress = data.ChangeSPaddress;
      postOfficeBox = data.PostOfficeBox;
      loadServicePointSelectionPage = true;
      if (shippingcountryISOCode != '')
        callGetShippingAddress = false;
    }
  });
});

//when the user selected the wrong servicepoint
function ChangeSPaddress() {
  $('input[type=radio][name="shippingmethod"]:checked').trigger("change");
}

//used for resetting and start over with service point items
function ResetSPdata() {
  $('#result').empty();
  $('#ROPCServicePointAddress').empty();
  document.getElementById('SMROPCspsection').style.display = "none";
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

function openServicePointPicker(apiKey, country, language, postalCode, carriers, servicePointId, postNumber) {
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

        //get the servicepoint id
        var obj = JSON.parse(Sendcloudresultelement.innerHTML);
        var spidreceived = obj.id;
        //console.log('servicepointid: ' + spidreceived);

        //get the service point carrier value
        var spcarrierreceived = obj.carrier;

        //get the ponumber value entered by the user
        spponumber = postNumber;

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
          //spponumber = spponumber.split(" ")[0];
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
        sessionStorage.setItem("sessionservicepointlat", splat);
        sessionStorage.setItem("sessionservicepointlong", splong);
        sessionStorage.setItem("sessionservicepointponumber", spponumber);

        loadServicePointSelectionPage = true;
        callGetShippingAddress = true;

      }

    },
    function (errors) {
      // Servier point selection canceled
      ResetSessionData()
      // Select first radio button
      // Select first radio button
      radiobtn = document.getElementById("shippingmethod_0");
      radiobtn.checked = true;
      errors.forEach(function (error) {
        console.log('Failure callback, reason: ' + error);
      })
    }
  )
}