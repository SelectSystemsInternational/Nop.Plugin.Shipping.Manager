using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Infrastructure;
using Nop.Services.Orders;
using Nop.Services.Shipping;

using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Models;

namespace Nop.Plugin.Shipping.Manager.Services
{
    /// <summary>
    /// Shipping service
    /// </summary>
    public partial class CustomShippingService : ShippingService
    {

        #region Utilities


        /// <summary>
        /// Remove an item from the package list
        /// </summary>
        /// <param name="items">List of package options</param>
        /// <param name="item">Item to remove</param>
        /// <returns>The package list with the item removed</returns>
        public bool IsSimpleRequest(GetShippingOptionRequest shippingOptionRequest)
        {
            bool first = true;
            bool simpleCalculation = true;
            int warehouseId = 0;
            int vendorId = 0;

            foreach (var item in shippingOptionRequest.Items)
            {
                if (first)
                {
                    warehouseId = item.Product.WarehouseId;
                    vendorId = item.Product.VendorId;
                    first = false;
                }

                if (item.Product.WarehouseId != warehouseId ||
                    item.Product.VendorId != vendorId)
                {
                    simpleCalculation = false;
                    break;
                }
            }

            return simpleCalculation;
        }

        /// <summary>
        /// Remove an item from the package list
        /// </summary>
        /// <param name="items">List of package options</param>
        /// <param name="item">Item to remove</param>
        /// <returns>The package list with the item removed</returns>
        public List<GetShippingOptionRequest.PackageItem> RemoveItem(IList<GetShippingOptionRequest.PackageItem> items,
            GetShippingOptionRequest.PackageItem removeItem)
        {
            var list = new List<GetShippingOptionRequest.PackageItem>();
            foreach (var i in items)
            {
                if (i.Product.Id == removeItem.Product.Id)
                    list.Add(removeItem);
            }
            return list.ToList();
        }

        /// <summary>
        /// Remove an item from the package list
        /// </summary>
        /// <param name="items">List of package options</param>
        /// <param name="item">Item to remove</param>
        /// <returns>The package list with the item removed</returns>
        public List<GetShippingOptionRequest.PackageItem> AddItem(GetShippingOptionRequest.PackageItem newItem)
        {
            var list = new List<GetShippingOptionRequest.PackageItem>();
            list.Add(newItem);
            return list.ToList();
        }

        /// <summary>
        /// Copy a list request
        /// </summary>
        /// <param name="request">Shipping Option Request</param>
        /// <returns>a copy of the Shipping Option Request/// </returns>
        public GetShippingOptionRequest CopyShippingOptionRequest(GetShippingOptionRequest request, bool copyItems = true)
        {
            //copy.Items = new List<PackageItem>();
            var copy = new GetShippingOptionRequest();

            copy.Customer = request.Customer;
            copy.ShippingAddress = request.ShippingAddress;
            copy.WarehouseFrom = request.WarehouseFrom;
            copy.CountryFrom = request.CountryFrom;
            copy.StateProvinceFrom = request.StateProvinceFrom;
            copy.ZipPostalCodeFrom = request.ZipPostalCodeFrom;
            copy.CountyFrom = request.CountyFrom;
            copy.CityFrom = request.CityFrom;
            copy.AddressFrom = request.AddressFrom;
            copy.StoreId = request.StoreId;

            if (copyItems)
            {
                foreach (var r in request.Items)
                    copy.Items.Add(r);
            }
            else
                copy.Items = new List<GetShippingOptionRequest.PackageItem>();

            return copy;
        }

        /// <summary>
        /// Checks if the package needs to be shipped seperately 
        /// </summary>
        /// <param name="smco">Shipping Manager Calculation Option</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result is true if items are to be shipped seperatly
        public async Task<bool> ShipSeperately(ShippingManagerCalculationOption smco)
        {
            bool shipSeparately = false;
            foreach (var sor in smco.Sor)
                foreach (var sci in sor.Items)
                    if ((await _productService.GetProductByIdAsync(sci.ShoppingCartItem.ProductId)).ShipSeparately)
                        shipSeparately = true;

            return shipSeparately;
        }

        #endregion

        #region Shipping Method Option Processing

        /// <summary>
        /// Create list of shipping method requests for a list of shipping option requests by product
        /// </summary>
        /// <param name="shippingOptionRequests">List of shipping option requests</param>
        /// <param name="storeId">Load records allowed only in a specified store; pass 0 to load all records</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping manager calculation options
        /// </returns>
        public virtual async Task<List<ShippingManagerCalculationOptions>> CreateRequestsListByProductAsync(IList<GetShippingOptionRequest> shippingOptionRequests, IList<ShoppingCartItem> cart, int storeId)
        {
            //get subtotals of shipped items
            var subTotal = decimal.Zero;
            var weight = decimal.Zero;
            int warehouseId = 0;
            int vendorId = 0;

            var srcmList = new List<ShippingManagerCalculationOptions>();
            var srcmResultList = new List<ShippingManagerCalculationOptions>();

            var shoppingCartService = EngineContext.Current.Resolve<IShoppingCartService>();
            var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();

            //get subtotal of all shopping cart items (including items with free shipping)
            foreach (var item in cart)
                subTotal += (await shoppingCartService.GetSubTotalAsync(item, false)).subTotal;

            if (_shippingManagerSettings.TestMode)
            {
                string message = "Shipping Manager - Create Request Lists By Product For Store : " + storeId +
                    " Requests Count: " + shippingOptionRequests.Count().ToString() + " Cart Qty: " + cart.Count().ToString() + 
                    " for Subtotal: " + subTotal.ToString();
                await _logger.InsertLogAsync(LogLevel.Information, message);
            }

            foreach (var shippingOptionRequest in shippingOptionRequests)
            {
                //get weight of shipped items (excluding items with free shipping)
                weight = await GetTotalWeightAsync(shippingOptionRequest, ignoreFreeShippedItems: true);

                if (IsSimpleRequest(shippingOptionRequest))
                {
                    var countryId = shippingOptionRequest.ShippingAddress.CountryId ?? 0;
                    var stateProvinceId = shippingOptionRequest.ShippingAddress.StateProvinceId ?? 0;
                    var zip = shippingOptionRequest.ShippingAddress.ZipPostalCode;
                    warehouseId = shippingOptionRequest.WarehouseFrom?.Id ?? 0;
                    vendorId = shippingOptionRequest.Items.First().Product.VendorId;

                    if (_shippingManagerSettings.TestMode)
                    {
                        string message = "Shipping Manager - Create Shipping Method Requests > CountryId: " + 
                            countryId.ToString() + " StateId: " + stateProvinceId.ToString() + " Zip: " + zip +
                            " WarehouseId: " + warehouseId.ToString() + " VendorId: " + vendorId.ToString() +
                            " > Option Requests Count: " + shippingOptionRequest.Items.Count;
                        await _logger.InsertLogAsync(LogLevel.Information, message);
                    }

                    var foundRecords = await shippingManagerService.GetRecordsAsync(0, storeId, vendorId, warehouseId, 0, countryId, stateProvinceId, zip, weight, subTotal);

                    if (_shippingManagerSettings.TestMode)
                    {
                        if (_shippingManagerSettings.TestMode)
                        {
                            string message = "Shipping Manager - Create Shipping Method Requests > Records Found: " + foundRecords.Count();
                            await _logger.InsertLogAsync(LogLevel.Debug, message);

                            if (foundRecords.Count() == 0)
                            {
                                message = "Shipping Manager - Create Shipping Method Requests > " +
                                    "No Rate Records Found in Configuration";
                                await _logger.InsertLogAsync(LogLevel.Information, message);
                            }
                        }
                    }

                    foreach (var record in foundRecords)
                    {
                        var carrier = await _carrierService.GetCarrierByIdAsync(record.CarrierId);
                        if (carrier != null)
                        {
                            if (_shippingManagerSettings.TestMode)
                            {
                                string message = "Shipping Manager - Create Shipping Method Requests > ShippingMethodId: " +
                                    record.ShippingMethodId.ToString() + " For Carrier " + carrier.Name;
                                await _logger.InsertLogAsync(LogLevel.Information, message);
                            }

                            bool methodFound = false;
                            var srcmListSelected = srcmList.Where(l => l.ShippingRateComputationMethodSystemName == carrier.ShippingRateComputationMethodSystemName);
                            foreach (var srcm in srcmListSelected)
                            {
                                if (srcm.ShippingRateComputationMethodSystemName == carrier.ShippingRateComputationMethodSystemName)
                                {
                                    methodFound = true;
                                    srcm.Smcro.Add(CreateProductSmcro(shippingOptionRequest, carrier, 0, warehouseId, record, weight));
                                }
                            }

                            if (!methodFound)
                                srcmList.Add(CreateProductSmco(shippingOptionRequest, carrier, 0, record, weight, subTotal, true));
                        }
                        else
                        { 
                            string message = "Shipping Manager - Create Shipping Method Requests > No Carrier Found for Record";
                            await _logger.InsertLogAsync(LogLevel.Error, message);
                        }
                    }

                }
                else
                {
                    foreach (var item in shippingOptionRequest.Items)
                    {
                        warehouseId = shippingOptionRequest.WarehouseFrom?.Id ?? 0;
                        var countryId = shippingOptionRequest.ShippingAddress.CountryId ?? 0;
                        var stateProvinceId = shippingOptionRequest.ShippingAddress.StateProvinceId ?? 0;
                        var zip = shippingOptionRequest.ShippingAddress.ZipPostalCode;

                        var product = await _productService.GetProductByIdAsync(item.ShoppingCartItem.ProductId);
                        if (product != null)
                        {
                            vendorId = product.VendorId;

                            if (_shippingManagerSettings.TestMode)
                            {
                                string message = "Shipping Manager - Custom Shipping Service for Product > For Store : " + storeId +
                                    " Create Shipping Method Requests > CountryId: " + countryId.ToString() + " StateId: " + stateProvinceId.ToString() + " Zip: " + zip +
                                    " WarehouseId: " + warehouseId.ToString() + " VendorId: " + vendorId.ToString() +
                                    " > Option Requests Count: " + shippingOptionRequest.Items.Count;
                                await _logger.InsertLogAsync(LogLevel.Information, message);
                            }

                            var foundRecords = await shippingManagerService.GetRecordsAsync(0, storeId, vendorId, warehouseId, 0, countryId, stateProvinceId, zip, weight, subTotal);

                            if (_shippingManagerSettings.TestMode)
                            {
                                string message = "Shipping Manager - Custom Shipping Service for Product > For Store : " + storeId +
                                    " Create Shipping Method Requests > Records Found: " + foundRecords.Count() + " for Vendor: " + vendorId;
                                await _logger.InsertLogAsync(LogLevel.Debug, message);

                                if (foundRecords.Count() == 0)
                                {
                                    message = "Shipping Manager - Custom Shipping Service for Product > For Store : " + storeId +
                                        " Create Shipping Method Requests > No Rate Records Found in Configuration";
                                    await _logger.InsertLogAsync(LogLevel.Information, message);
                                }
                            }

                            foreach (var record in foundRecords)
                            {
                                var carrier = await _carrierService.GetCarrierByIdAsync(record.CarrierId);
                                if (carrier != null)
                                {
                                    bool methodFound = false;
                                    bool srcmAddListWarehouse = false;
                                    var srcmListSelected = srcmList.Where(l => l.ShippingRateComputationMethodSystemName == carrier.ShippingRateComputationMethodSystemName);
                                    foreach (var srcm in srcmListSelected)
                                    {
                                        if (srcm.ShippingRateComputationMethodSystemName == carrier.ShippingRateComputationMethodSystemName)
                                        {
                                            methodFound = true;
                                            bool productFound = false;

                                            var listProduct = srcm.Smcro.Select(i => i.ProductId == item.ShoppingCartItem.ProductId);
                                            if (listProduct.Any(p => p))
                                            {
                                                productFound = true;
                                                var productItem = srcm.Smcro.Where(i => i.ProductId == product.Id);

                                                if (productItem.Any(s => s.Smco.Any(r => r.Smbwtr.WarehouseId != record.WarehouseId)))
                                                {
                                                    // product is available from multiple warehouses
                                                    var warehouse = productItem.Select(s => s.Smco.Where(r => r.Smbwtr.WarehouseId != record.WarehouseId));
                                                    foreach (var request in warehouse)
                                                    {
                                                        srcmAddListWarehouse = srcmListSelected.Any(s => s.Smcro.Any(w => w.WarehouseId == record.WarehouseId));
                                                        if (srcmAddListWarehouse)
                                                        {
                                                            var srcmAddListRate = srcmResultList.Select(s => s.Smcro.Any(w => w.Smco.Any(r => r.Smbwtr == record)));
                                                            foreach (var addListRequest in srcmAddListRate)
                                                            {
                                                                srcmAddListWarehouse = false;
                                                                var requestProduct = request.Select(i => i.Sor.Select(r => r.Items.Select(r => r.ShoppingCartItem).Select(k => k.ProductId)));
                                                                var findProduct = shippingOptionRequest.Items.Select(r => r.ShoppingCartItem).Select(k => k.ProductId);
                                                                bool sameProduct = requestProduct.Any(y => y.Any() == findProduct.Any());
                                                                if (sameProduct)
                                                                {
                                                                    foreach (var q in request)
                                                                        q.Quantity++;
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            methodFound = false;
                                                        }
                                                    }
                                                }
                                                else if (productItem.Any(s => s.Smco.Any(r => r.Smbwtr.AdditionalFixedCost == record.AdditionalFixedCost)))
                                                {
                                                    var rate = productItem.Select(s => s.Smco.Where(r => r.Smbwtr.AdditionalFixedCost == record.AdditionalFixedCost));
                                                    foreach (var request in rate)
                                                    {
                                                        var requestProduct = request.Select(i => i.Sor.Select(r => r.Items.Select(r => r.ShoppingCartItem).Select(k => k.ProductId)));
                                                        var findProduct = shippingOptionRequest.Items.Select(r => r.ShoppingCartItem).Select(k => k.ProductId);
                                                        bool sameProduct = requestProduct.Any(y => y.Any() == findProduct.Any());
                                                        if (sameProduct)
                                                        {
                                                            foreach (var q in request)
                                                                q.Quantity++;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    var shippingManagerCalculationOption = new ShippingManagerCalculationOption();
                                                    shippingManagerCalculationOption.Smbwtr = record;
                                                    shippingManagerCalculationOption.Quantity = 1;

                                                    var sor = CopyShippingOptionRequest(shippingOptionRequest);
                                                    var selected = sor.Items.Select(i => i).Where(p => p.Product.Id == product.Id).FirstOrDefault();
                                                    if (selected != null)
                                                        sor.Items = RemoveItem(sor.Items, selected);

                                                    shippingManagerCalculationOption.Sor.Add(sor);

                                                    productItem.FirstOrDefault().Smco.Add(shippingManagerCalculationOption);
                                                }
                                            }

                                            if (!productFound)
                                                srcm.Smcro.Add(CreateProductSmcro(shippingOptionRequest, carrier, product.Id, product.WarehouseId, record, weight));
                                        }
                                    }

                                    if (!methodFound)
                                        srcmList.Add(CreateProductSmco(shippingOptionRequest, carrier, product.Id, record, weight, subTotal));
                                }
                                else
                                {
                                    string message = "Shipping Manager - Create Shipping Method Requests > No Carrier Found for Record";
                                    await _logger.InsertLogAsync(LogLevel.Error, message);
                                }
                            }
                        }
                    }
                }
            }

            // Check for errors
            foreach (var smrItem in srcmList)
            {
                string srcmName = smrItem.ShippingRateComputationMethodSystemName;

                if (smrItem.SimpleList && smrItem.Smcro.Count() > 0 || // To Do checks
                    !smrItem.SimpleList && smrItem.Smcro.Count() == cart.Count())
                    srcmResultList.Add(smrItem);
                else
                {
                    if (_shippingManagerSettings.TestMode)
                    {
                        // Only partital cost can be calculated for this method
                        string message = "Shipping Manager - Vendor methods do not match cart items" + smrItem.ShippingRateComputationMethodSystemName;
                        await _logger.InsertLogAsync(LogLevel.Information, message);
                    }
                }
            }

            return srcmResultList;
        }

        /// <summary>
        /// Create list of shipping method requests for a list of shipping option requests by warehouse
        /// </summary>
        /// <param name="shippingOptionRequests">List of shipping option requests</param>
        /// <param name="storeId">Load records allowed only in a specified store; pass 0 to load all records</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping manager calculation options
        /// </returns>
        public virtual async Task<List<ShippingManagerCalculationOptions>> CreateRequestsListByWarehouseAsync(IList<GetShippingOptionRequest> shippingOptionRequests, IList<ShoppingCartItem> cart, int storeId)
        {
            //get subtotals of shipped items
            var subTotal = decimal.Zero;
            var weight = decimal.Zero;

            var srcmList = new List<ShippingManagerCalculationOptions>();
            var srcmResultList = new List<ShippingManagerCalculationOptions>();

            var shoppingCartService = EngineContext.Current.Resolve<IShoppingCartService>();
            var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();

            //get subtotal of all shopping cart items (including items with free shipping)
            foreach (var item in cart)
                subTotal += (await shoppingCartService.GetSubTotalAsync(item, false)).subTotal;

            //get weight of shipped items (excluding items with free shipping)
            foreach (var shippingOptionRequest in shippingOptionRequests)
                weight += await GetTotalWeightAsync(shippingOptionRequest, ignoreFreeShippedItems: true);

            if (_shippingManagerSettings.TestMode)
            {
                string message = "Shipping Manager - Create Request Lists By Product For Store : " + storeId +
                    " Requests Count: " + shippingOptionRequests.Count().ToString() + " Cart Qty: " + cart.Count().ToString() + 
                    " for Subtotal: " + subTotal.ToString() + " for Weight " + weight.ToString();
                await _logger.InsertLogAsync(LogLevel.Information, message);
            }

            foreach (var shippingOptionRequest in shippingOptionRequests)
            {
                var warehouseId = shippingOptionRequest.WarehouseFrom?.Id ?? 0;
                var countryId = shippingOptionRequest.ShippingAddress.CountryId ?? 0;
                var stateProvinceId = shippingOptionRequest.ShippingAddress.StateProvinceId ?? 0;
                var zip = shippingOptionRequest.ShippingAddress.ZipPostalCode;

                int vendorId = 0;
                var product = await _productService.GetProductByIdAsync(shippingOptionRequest.Items.FirstOrDefault().Product.Id);
                if (product != null)
                {
                    vendorId = product.VendorId;
                    var vendor = await _vendorService.GetVendorByIdAsync(vendorId);
                    if (vendor == null || vendor.Deleted || !vendor.Active)
                        vendorId = 0;

                    int listWarehouseId = 0;
                    var listWarehouse = (await _warehouseRepository.GetByIdAsync(warehouseId));
                    if (listWarehouse != null)
                        listWarehouseId = listWarehouse.Id;

                    if (_shippingManagerSettings.TestMode)
                    {
                        string message = "Shipping Manager - Create Shipping Method Requests > Product : " +
                            product.Name + " CountryId: " + countryId.ToString() + 
                            " StateId: " + stateProvinceId.ToString() + " Zip: " + zip +
                            " WarehouseId: " + listWarehouseId.ToString() + " VendorId: " + vendorId.ToString() +
                            " > Option Requests Count: " + shippingOptionRequest.Items.Count;
                        await _logger.InsertLogAsync(LogLevel.Information, message);
                    }

                    var selected = shippingOptionRequest.Items.Select(i => i).Where(p => p.Product.WarehouseId == listWarehouseId);
                    if (selected.Count() > 0)
                    {
                        var foundRecords = await shippingManagerService.GetRecordsAsync(0, storeId, vendorId, warehouseId, 0, countryId, stateProvinceId, zip, weight, subTotal);

                        if (_shippingManagerSettings.TestMode)
                        {
                            string message = "Shipping Manager - Create Shipping Method Requests > Records Found: " + foundRecords.Count();
                            await _logger.InsertLogAsync(LogLevel.Debug, message);

                            if (foundRecords.Count() == 0)
                            {
                                message = "Shipping Manager - Create Shipping Method Requests > " +
                                    "No Rate Records Found in Configuration";
                                await _logger.InsertLogAsync(LogLevel.Information, message);
                            }
                        }

                        foreach (var record in foundRecords)
                        {
                            var carrier = await _carrierService.GetCarrierByIdAsync(record.CarrierId);
                            if (carrier != null)
                            {
                                bool calculationMethodFound = false;
                                foreach (var srcm in srcmList)
                                {
                                    if (_shippingManagerSettings.TestMode)
                                    {
                                        string message = "Shipping Manager - Create Shipping Method Requests > ShippingMethodId: " +
                                            record.ShippingMethodId.ToString() + " For Carrier " + carrier.Name;
                                        await _logger.InsertLogAsync(LogLevel.Information, message);
                                    }

                                    if (srcm.ShippingRateComputationMethodSystemName == carrier.ShippingRateComputationMethodSystemName)
                                    {
                                        calculationMethodFound = true;
                                        bool warehouseFound = false;
                                       
                                        if (calculationMethodFound)
                                        {
                                            bool shippingMethodFound = srcm.Smcro.Select(i => i.Smco).Where(smco => smco.Any(s => s.Smbwtr.ShippingMethodId == record.ShippingMethodId)).Any();

                                            if (shippingMethodFound)
                                            {
                                                var foundWarehouse = srcm.Smcro.Select(i => i.WarehouseId == listWarehouseId);
                                                if (foundWarehouse.Any(w => w))
                                                {
                                                    warehouseFound = true;
                                                    var productItem = srcm.Smcro.Where(i => i.WarehouseId == listWarehouseId);

                                                    if (productItem.Any(s => s.Smco.Any(r => r.Smbwtr.WarehouseId != record.WarehouseId)))
                                                    {
                                                        if (record.WarehouseId == 0 || record.WarehouseId == listWarehouseId)
                                                            srcmResultList.Add(CreateWarehouseSmco(shippingOptionRequest, selected, carrier, listWarehouseId, record, weight, subTotal));
                                                    }
                                                    else
                                                    {
                                                        foreach (var pi in productItem)
                                                            foreach (var a in pi.Smco)
                                                                AppendSor(shippingOptionRequest, selected, pi);
                                                    }
                                                }

                                                if (!warehouseFound)
                                                {
                                                    selected = shippingOptionRequest.Items.Select(i => i).Where(p => p.Product.WarehouseId == listWarehouseId);
                                                    if (selected.Count() > 0)
                                                        srcm.Smcro.Add(CreateWarehouseSmcro(shippingOptionRequest, srcm, selected, listWarehouseId, record, weight));
                                                }
                                            }
                                            else
                                            {
                                                selected = shippingOptionRequest.Items.Select(i => i).Where(p => p.Product.WarehouseId == listWarehouseId);
                                                if (selected.Count() > 0)
                                                    srcm.Smcro.Add(CreateWarehouseSmcro(shippingOptionRequest, srcm, selected, listWarehouseId, record, weight));
                                            }
                                        }
                                    }
                                }

                                if (!calculationMethodFound)
                                {
                                    selected = shippingOptionRequest.Items.Select(i => i).Where(p => p.Product.WarehouseId == listWarehouseId);
                                    if (selected.Count() > 0)
                                        srcmList.Add(CreateWarehouseSmco(shippingOptionRequest, selected, carrier, listWarehouseId, record, weight, subTotal));
                                }
                            }
                            else
                            {
                                string message = "Shipping Manager - Create Shipping Method Requests > No Carrier Found for Record";
                                await _logger.InsertLogAsync(LogLevel.Error, message);
                            }
                        }
                    }
                }
            }

            foreach (var addMethod in srcmResultList)
                srcmList.Add(addMethod);

            // Check for errors in the options requests prepared
            srcmResultList = new List<ShippingManagerCalculationOptions>();
            foreach (var smrItem in srcmList)
            {
                int listWarehouseId = 0;
                srcmResultList.Add(smrItem);

                var warehouseFoundList = new List<Warehouse>();

                if (_shippingManagerSettings.TestMode)
                {
                    string message = "Shipping Manager - Custom Shipping Service for Warehouse > For Store : " + storeId +
                        " SRCM System Name: " + smrItem.ShippingRateComputationMethodSystemName + " SMRItem Count: " + smrItem.Smcro.Count() + " + " + "Item List Count: " + cart.Count();
                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                }

                foreach (var request in shippingOptionRequests)
                {
                    if (request.WarehouseFrom != null)
                        listWarehouseId = request.WarehouseFrom.Id;

                    foreach (var warehouse in smrItem.Smcro)
                    {
                        var searchWarehouse = await _warehouseRepository.GetByIdAsync(warehouse.WarehouseId);
                        if (searchWarehouse == null)
                        {
                            searchWarehouse = new Warehouse();
                            searchWarehouse.Name = "No Warehouse";
                            searchWarehouse.Id = 0;
                        }

                        if (!warehouseFoundList.Any(x => x.Id == searchWarehouse.Id))
                            warehouseFoundList.Add(searchWarehouse);

                        //if (searchWarehouse != null)
                        //{
                        //    if (listWarehouseId != warehouse.WarehouseId)
                        //        warehouseFoundList.Remove(searchWarehouse);
                        //}
                        //else
                        //    warehouseFoundList.Remove(searchWarehouse);
                    }
                }

                if (warehouseFoundList.Count() != shippingOptionRequests.Count())
                {
                    foreach (var warehouse in warehouseFoundList)
                    {
                        // Only partital cost can be calculated

                        string message = "Shipping Manager - Check rate options for " + smrItem.ShippingRateComputationMethodSystemName +
                            " for Warehouse: " + warehouse.Name;
                        await _logger.InsertLogAsync(LogLevel.Information, message);
                        srcmResultList.Remove(smrItem);
                    }
                }
            }

            return srcmResultList;
        }

        private void AppendSor(GetShippingOptionRequest sorItem, IEnumerable<GetShippingOptionRequest.PackageItem> selected,
            ShippingManagerCalculationRequestOption requestItem)
        {

            var sor = CopyShippingOptionRequest(sorItem, false);
            foreach (var item in selected)
                sor.Items.Add(item);

            foreach (var smco in requestItem.Smco)
                smco.Sor.Add(sor);
        }

        public ShippingManagerCalculationRequestOption CreateProductSmcro(GetShippingOptionRequest gsor,
            Carrier carrier, int productId, int warehouseId, ShippingManagerByWeightByTotal record, decimal weight)
        {
            var shippingManagerCalculationRequestOption = new ShippingManagerCalculationRequestOption();
            shippingManagerCalculationRequestOption.ProductId = productId;
            shippingManagerCalculationRequestOption.WarehouseId = warehouseId;
            shippingManagerCalculationRequestOption.Weight = weight;

            var shippingManagerCalculationOption = new ShippingManagerCalculationOption();
            shippingManagerCalculationOption.Smbwtr = record;
            shippingManagerCalculationOption.Quantity = 1;

            var sor = CopyShippingOptionRequest(gsor);
            var selected = sor.Items.Select(i => i).Where(p => p.Product.Id == productId).FirstOrDefault();
            if (selected != null)
                sor.Items = RemoveItem(sor.Items, selected);

            shippingManagerCalculationOption.Sor.Add(sor);

            shippingManagerCalculationRequestOption.Smco.Add(shippingManagerCalculationOption);

            return shippingManagerCalculationRequestOption;
        }

        public ShippingManagerCalculationRequestOption CreateWarehouseSmcro(GetShippingOptionRequest sorItem, ShippingManagerCalculationOptions srcm,
            IEnumerable<GetShippingOptionRequest.PackageItem> selected, int warehouseId, ShippingManagerByWeightByTotal record, decimal weight)
        {
            var shippingManagerCalculationRequestOption = new ShippingManagerCalculationRequestOption();
            shippingManagerCalculationRequestOption.WarehouseId = warehouseId;
            shippingManagerCalculationRequestOption.ProductId = 0;

            var shippingManagerCalculationOption = new ShippingManagerCalculationOption();
            shippingManagerCalculationOption.Smbwtr = record;
            shippingManagerCalculationOption.Quantity = 1;
            shippingManagerCalculationRequestOption.Weight = weight;

            var sor = CopyShippingOptionRequest(sorItem, false);
            foreach (var item in selected)
                sor.Items.Add(item);

            shippingManagerCalculationOption.Sor.Add(sor);
            shippingManagerCalculationRequestOption.Smco.Add(shippingManagerCalculationOption);

            return shippingManagerCalculationRequestOption;
        }

        public ShippingManagerCalculationOptions CreateProductSmco(GetShippingOptionRequest sorItem,
            Carrier carrier, int productId, ShippingManagerByWeightByTotal record, decimal weight, decimal subTotal, bool simpleList = false)
        {
            var smco = new ShippingManagerCalculationOptions();
            smco.SubTotal = subTotal;
            smco.ShippingRateComputationMethodSystemName = carrier.ShippingRateComputationMethodSystemName;
            smco.SimpleList = simpleList;

            var shippingManagerCalculationRequestOption = new ShippingManagerCalculationRequestOption();
            shippingManagerCalculationRequestOption.ProductId = productId;
            shippingManagerCalculationRequestOption.WarehouseId = record.WarehouseId;
            shippingManagerCalculationRequestOption.Weight = weight;

            var shippingManagerCalculationOption = new ShippingManagerCalculationOption();
            shippingManagerCalculationOption.Smbwtr = record;
            shippingManagerCalculationOption.Quantity = 1;

            var sor = CopyShippingOptionRequest(sorItem);
            var selected = sor.Items.Select(i => i).Where(p => p.Product.Id == productId).FirstOrDefault();
            if (selected != null)
                sor.Items = RemoveItem(sor.Items, selected);

            shippingManagerCalculationOption.Sor.Add(sor);

            shippingManagerCalculationRequestOption.Smco.Add(shippingManagerCalculationOption);

            smco.Smcro.Add(shippingManagerCalculationRequestOption);

            return smco;
        }

        public ShippingManagerCalculationOptions CreateWarehouseSmco(GetShippingOptionRequest sorItem,
            IEnumerable<GetShippingOptionRequest.PackageItem> selected,
            Carrier carrier, int warehouseId, ShippingManagerByWeightByTotal record, decimal weight, decimal subTotal)
        {

            var smco = new ShippingManagerCalculationOptions();

            smco.ShippingRateComputationMethodSystemName = carrier.ShippingRateComputationMethodSystemName;
            smco.SubTotal = subTotal;

            var shippingManagerCalculationRequestOption = new ShippingManagerCalculationRequestOption();
            shippingManagerCalculationRequestOption.WarehouseId = warehouseId;
            shippingManagerCalculationRequestOption.Weight = weight;

            var shippingManagerCalculationOption = new ShippingManagerCalculationOption();
            shippingManagerCalculationOption.Smbwtr = record;
            shippingManagerCalculationOption.Quantity = 1;

            var sor = CopyShippingOptionRequest(sorItem, false);

            foreach (var item in selected)
                sor.Items.Add(item);

            shippingManagerCalculationOption.Sor.Add(sor);
            shippingManagerCalculationRequestOption.Smco.Add(shippingManagerCalculationOption);

            smco.Smcro.Add(shippingManagerCalculationRequestOption);

            return smco;
        }

        /// <summary>
        /// Combine the current Shipping Option Response with a list of new Shipping Option Responses
        /// </summary>
        /// <param name="currentResponses">Get Shipping Option Response</param>
        /// <param name="additionalResponses">Get Shipping Option Response</param>
        /// <param name="storeId">Load records allowed only in a specified store; pass 0 to load all records</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping option response
        /// </returns> 
        public virtual async Task<GetShippingOptionResponse> GetShippingMethodOptionResponsesAsync(GetShippingOptionResponse currentResponses,
            GetShippingOptionResponse additionalResponses, int storeId)
        {
            bool found = false;

            if (!found)
            {
                foreach (var option in additionalResponses.ShippingOptions)
                {
                    if (_shippingManagerSettings.TestMode)
                    {
                        string message = "Shipping Manager - Get Shipping Method Option Response > Name: " + option.Name.Trim() + " Rate: " + option.Rate.ToString();
                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }

                    currentResponses.ShippingOptions.Add(option);
                }
            }

            foreach (var error in additionalResponses.Errors)
            {
                if (_shippingManagerSettings.TestMode)
                {
                    string message = "Shipping Manager - Get Shipping Method > Option Response > Error: " + error;
                    await _logger.InsertLogAsync(LogLevel.Information, message);
                }

                currentResponses.Errors.Add(error);
            }

            currentResponses.ShippingFromMultipleLocations = additionalResponses.ShippingFromMultipleLocations;

            return currentResponses;
        }

        /// <summary>
        /// Combine the current Shipping Option Response with a list of new Shipping Option Responses
        /// </summary>
        /// <param name="currentResponses">Get Shipping Option Response</param>
        /// <param name="additionalResponses">Get Shipping Option Response</param>
        /// <param name="storeId">Load records allowed only in a specified store; pass 0 to load all records</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping option response
        /// </returns> 
        public virtual async Task<GetShippingOptionResponse> ShippingOptionCombineResponsesAsync(GetShippingOptionResponse currentResponses, //Todo description and transit
            GetShippingOptionResponse additionalResponses, int storeId, bool combine = false)
        {

            var tempResult = new List<ShippingOption>();
            var combineResult = new List<ShippingOption>();

            bool found = false;

            foreach (var additionalOption in additionalResponses.ShippingOptions)
            {
                foreach (var currentOption in currentResponses.ShippingOptions)
                {
                    if (currentOption.Name.Contains(additionalOption.Name) || additionalOption.Name.Contains(currentOption.Name))
                    {

                        if (_shippingManagerSettings.TestMode)
                        {
                            string message = "Shipping Manager - Shipping Option Combine Response > Rate Found : " + additionalOption.Name + " in " + currentOption.Name;
                            await _logger.InsertLogAsync(LogLevel.Debug, message);
                        }

                        found = true;
                        currentOption.Rate += additionalOption.Rate;
                    }
                }

                if (!found && !combine)
                {
                    tempResult.Add(additionalOption);
                }

                if (combine)
                {
                    combineResult.Add(additionalOption);
                }
            }


            if (combine)
            {
                foreach (var additionalOption in combineResult)
                {
                    foreach (var response in currentResponses.ShippingOptions)
                    {
                        if (_shippingManagerSettings.TestMode)
                        {
                            string message = "Shipping Manager - Shipping Option Combine Response > " + additionalOption.Name + " with " + response.Name;
                            await _logger.InsertLogAsync(LogLevel.Debug, message);
                        }

                        if (additionalOption.Name.Contains("Express") || additionalOption.Name.Contains("Regular"))
                        {
                            if (response.Name.Contains("Express") && additionalOption.Name.Contains("Express"))
                            {
                                response.Name += " + " + additionalOption.Name;
                                response.Rate += additionalOption.Rate;
                            }
                            else if (response.Name.Contains("Regular") && additionalOption.Name.Contains("Regular"))
                            {
                                response.Name += " + " + additionalOption.Name;
                                response.Rate += additionalOption.Rate;
                            }
                        }
                        else
                        {
                            response.Name += " + " + additionalOption.Name;
                            response.Rate += additionalOption.Rate;
                        }
                    }
                }
            }

            if (tempResult.Any())
            {
                if (currentResponses.ShippingOptions.Count == 0)
                {
                    foreach (var option in additionalResponses.ShippingOptions)
                    {
                        if (_shippingManagerSettings.TestMode)
                        {
                            string message = "Shipping Manager - Shipping Option Combine Response > Rate Added : " + option.Name + " Rate: " + option.Rate.ToString();
                            await _logger.InsertLogAsync(LogLevel.Debug, message);
                        }

                        currentResponses.ShippingOptions.Add(option);
                    }
                }
                else
                {
                    var tempCombination = new GetShippingOptionResponse();
                    foreach (var result in tempResult)
                    {
                        tempCombination.ShippingOptions.Add(result);
                    }
                    currentResponses = await ShippingOptionCombineResponsesAsync(currentResponses, tempCombination, storeId, true);
                }
            }
            else
            {
                foreach (var error in additionalResponses.Errors)
                {
                    if (_shippingManagerSettings.TestMode)
                    {
                        string message = "Shipping Manager - ShippingOptionCombineResponse - Error: " + error;
                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }

                    currentResponses.Errors.Add(error);
                }

                currentResponses.ShippingFromMultipleLocations = additionalResponses.ShippingFromMultipleLocations;
            }

            return currentResponses;

        }

        #endregion

        #region Shipping.Manager 

        /// <summary>
        /// Get both the single and package options for Shipping Method Shipping Service
        /// </summary>
        /// <param name="srcm">Shipping Rate Computation Method</param>
        /// <param name="smco">Shipping Manager Calculation Option</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping option response
        /// </returns> 
        public virtual async Task GetShippingMethodResponsesForProductAsync(IShippingRateComputationMethod srcm, ShippingManagerCalculationRequestOption requestOption,
            ShippingManagerCalculationOption smco, int storeId)
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            var customer = await _workContext.GetCurrentCustomerAsync();

            var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();

            if (await ShipSeperately(smco))
            {
                //rather than request to the get a rate for each package - request the rate once and multiply by quantity

                foreach (var so in smco.Sor)
                    so.Items.FirstOrDefault().ShoppingCartItem.Quantity = 1;

                await _genericAttributeService.SaveAttributeAsync<int>(customer, ShippingManagerDefaults.CURRENT_SHIPPING_METHOD_SELECTOR, smco.Smbwtr.ShippingMethodId, storeId);

                var shippingMethodOptions = await GetShippingMethodOptionsAsync(srcm, smco.Sor);

                if (_shippingManagerSettings.TestMode)
                {
                    string message = "Shipping Manager - Custom Shipping Service > For Store : " + storeId +
                        " SM Ship Seperately > Shipping Method Option for Package Count: " + shippingMethodOptions.ShippingOptions.Count +
                        " for Product: " + requestOption.ProductId.ToString() +
                        " Error Count: " + shippingMethodOptions.Errors.Count();
                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                }

                if (shippingMethodOptions.Errors.Count() == 0)
                {
                    foreach (var option in shippingMethodOptions.ShippingOptions)
                    {
                        option.Rate = await _priceCalculationService.RoundPriceAsync(option.Rate * smco.Quantity);

                        var formatedOption = await shippingManagerService.FormatOptionDetails(option, smco);
                        option.Name = formatedOption.Name;
                        option.Description = formatedOption.Description;
                        option.TransitDays = formatedOption.TransitDays;
                        option.DisplayOrder = formatedOption.DisplayOrder;
                    }
                }
                else
                {
                    foreach (var option in shippingMethodOptions.ShippingOptions)
                        option.Rate = 0;

                    var p = await _productService.GetProductByIdAsync(requestOption.ProductId);
                    if (p != null)
                    {
                        for (int i = 0; i < shippingMethodOptions.Errors.Count(); i++)
                            shippingMethodOptions.Errors[i] += " for Product " + p.Name;
                    }
                }

                requestOption.Gsor = await ShippingOptionCombineResponsesAsync(requestOption.Gsor, shippingMethodOptions, storeId);

                foreach (var so in smco.Sor)
                    so.Items.FirstOrDefault().ShoppingCartItem.Quantity = smco.Quantity;
            }
            else
            {
                // request the rate for a package

                await _genericAttributeService.SaveAttributeAsync<int>(customer, ShippingManagerDefaults.CURRENT_SHIPPING_METHOD_SELECTOR, smco.Smbwtr.ShippingMethodId, storeId);

                var packageShippingMethodOptions = await GetShippingMethodOptionsAsync(srcm, smco.Sor);

                if (_shippingManagerSettings.TestMode)
                {
                    string message = "Shipping Manager - Custom Shipping Service > For Store : " + storeId +
                        " SM Ship Package > Shipping Method Option for Package Count: " + packageShippingMethodOptions.ShippingOptions.Count +
                        " for Plugin: " + requestOption + srcm.PluginDescriptor.ToString() +
                        " for Product: " + requestOption.ProductId.ToString() +
                        " Error Count: " + packageShippingMethodOptions.Errors.Count();
                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                }

                if (packageShippingMethodOptions.Errors.Count() == 0)
                {
                    foreach (var option in packageShippingMethodOptions.ShippingOptions)
                    {
                        option.Rate = await _priceCalculationService.RoundPriceAsync(option.Rate);

                        var formatedOption = await shippingManagerService.FormatOptionDetails(option, smco);
                        option.Name = formatedOption.Name;
                        option.Description = formatedOption.Description;
                        option.TransitDays = formatedOption.TransitDays;
                        option.DisplayOrder = formatedOption.DisplayOrder;
                    }
                }
                else
                {
                    foreach (var option in packageShippingMethodOptions.ShippingOptions)
                        option.Rate = 0;

                    var p = await _productService.GetProductByIdAsync(requestOption.ProductId);
                    if (p != null)
                    {
                        for (int i = 0; i < packageShippingMethodOptions.Errors.Count(); i++)
                            packageShippingMethodOptions.Errors[i] += " for Product " + p.Name;
                    }
                }

                requestOption.Gsor = await ShippingOptionCombineResponsesAsync(requestOption.Gsor, packageShippingMethodOptions, storeId);
            }
        }

        /// <summary>
        /// Get both the single and package options for Shipping Method Shipping Service
        /// </summary>
        /// <param name="srcm">Shipping Rate Computation Method</param>
        /// <param name="smco">Shipping Manager Calculation Option</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping option response
        /// </returns> 
        public virtual async Task GetShippingMethodResponsesForWarehouseAsync(IShippingRateComputationMethod srcm, ShippingManagerCalculationRequestOption requestOption,
            ShippingManagerCalculationOption smco, int storeId)
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            var customer = await _workContext.GetCurrentCustomerAsync();

            MeasureWeight usedMeasureWeight = await GatewayMeasureWeightAsync();
            var requestSubtotal = new ShippingManagerCalculationRequestOption();

            var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();

            var shipSeperatley = smco.Sor.Where(i => i.Items.Any(s => s.OverriddenQuantity != null));
            var shipTogether = smco.Sor.Where(i => i.Items.Any(s => s.OverriddenQuantity == null));

            foreach (var request in smco.Sor)
            {
                bool shipItemSeperatey = request.Items.Any(i => i.OverriddenQuantity != null);

                if (shipItemSeperatey && smco.Sor.Count() > 1)
                {
                    // request the rate for ship seperately

                    await _genericAttributeService.SaveAttributeAsync<int>(customer, ShippingManagerDefaults.CURRENT_SHIPPING_METHOD_SELECTOR, smco.Smbwtr.ShippingMethodId, storeId);

                    var shippingMethodOptions = await srcm.GetShippingOptionsAsync(request);

                    if (_shippingManagerSettings.TestMode)
                    {
                        string message = "Shipping Manager - Custom Shipping Service > For Store : " + storeId +
                            " SM Ship Seperately > Shipping Method Option for Package Count: " + shippingMethodOptions.ShippingOptions.Count +
                            " for Warehouse: " + requestOption.WarehouseId.ToString() +
                            " Error Count: " + shippingMethodOptions.Errors.Count();
                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }

                    if (shippingMethodOptions.Errors.Count() == 0)
                    {
                        foreach (var option in shippingMethodOptions.ShippingOptions)
                        {
                            option.Rate = await _priceCalculationService.RoundPriceAsync(option.Rate);

                            var formatedOption = await shippingManagerService.FormatOptionDetails(option, smco);
                            option.Name = formatedOption.Name;
                            option.Description = formatedOption.Description;
                            option.TransitDays = formatedOption.TransitDays;
                            option.DisplayOrder = formatedOption.DisplayOrder;
                        }
                    }
                    else
                    {
                        foreach (var option in shippingMethodOptions.ShippingOptions)
                            option.Rate = 0;

                        string warehouseName = "No Warehouse";
                        var warehouse = await GetWarehouseByIdAsync(requestOption.WarehouseId);
                        if (warehouse != null)
                            warehouseName = warehouse.Name;

                        for (int i = 0; i < shippingMethodOptions.Errors.Count(); i++)
                            shippingMethodOptions.Errors[i] += " for warehouse: " + warehouseName;
                    }

                    requestSubtotal.Gsor = await ShippingOptionCombineResponsesAsync(requestSubtotal.Gsor, shippingMethodOptions, storeId);
                }
                else
                {
                    // request the rate for a package

                    await _genericAttributeService.SaveAttributeAsync<int>(customer, ShippingManagerDefaults.CURRENT_SHIPPING_METHOD_SELECTOR, smco.Smbwtr.ShippingMethodId, storeId);

                    var packageShippingMethodOptions = await srcm.GetShippingOptionsAsync(request);

                    if (_shippingManagerSettings.TestMode)
                    {
                        string message = "Shipping Manager - Custom Shipping Service > For Store : " + storeId +
                            " SM Ship Package > Shipping Method Option for Package Count: " + packageShippingMethodOptions.ShippingOptions.Count +
                            " for Plugin: " + requestOption + srcm.PluginDescriptor.ToString() +
                            " for Warehouse: " + requestOption.WarehouseId.ToString() +
                            " Error Count: " + packageShippingMethodOptions.Errors.Count();
                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }

                    if (packageShippingMethodOptions.Errors.Count() == 0)
                    {
                        foreach (var option in packageShippingMethodOptions.ShippingOptions)
                        {
                            option.Rate = await _priceCalculationService.RoundPriceAsync(option.Rate);

                            var formatedOption = await shippingManagerService.FormatOptionDetails(option, smco);
                            option.Name = formatedOption.Name;
                            option.Description = formatedOption.Description;
                            option.TransitDays = formatedOption.TransitDays;
                            option.DisplayOrder = formatedOption.DisplayOrder;
                        }
                    }
                    else
                    {
                        foreach (var option in packageShippingMethodOptions.ShippingOptions)
                            option.Rate = 0;

                        string warehouseName = "No Warehouse";
                        var warehouse = await GetWarehouseByIdAsync(requestOption.WarehouseId);
                        if (warehouse != null)
                            warehouseName = warehouse.Name;

                        for (int i = 0; i < packageShippingMethodOptions.Errors.Count(); i++)
                            packageShippingMethodOptions.Errors[i] += " for warehouse: " + warehouseName;
                    }

                    requestOption.Gsor = await ShippingOptionCombineResponsesAsync(requestOption.Gsor, packageShippingMethodOptions, storeId);

                }
            }

            if (requestSubtotal.Gsor.ShippingOptions.Count() > 0)
                requestOption.Gsor = await ShippingOptionCombineResponsesAsync(requestOption.Gsor, requestSubtotal.Gsor, storeId);
        }

        #endregion

        #region Shipping.Plugins

        /// <summary>
        /// Get both the single and package options for External Shipping Computation Methods request list organise by product
        /// </summary>
        /// <param name="srcm">Shipping Rate Computation Method</param>
        /// <param name="smco">Shipping Manager Calculation Option</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping option response
        /// </returns> 
        public virtual async Task GetShippingCalculationMethodResponsesForProductAsync(IShippingRateComputationMethod srcm,
            ShippingManagerCalculationRequestOption requestOption, ShippingManagerCalculationOption smco, int storeId)
        {
            MeasureWeight usedMeasureWeight = await GatewayMeasureWeightAsync();

            var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();

            if (await ShipSeperately(smco))
            {
                //rather than request to the get a rate for each package - request the rate once and multiply by quantity

                foreach (var so in smco.Sor)
                    so.Items.FirstOrDefault().ShoppingCartItem.Quantity = 1;

                var shippingMethodOptions = await GetShippingMethodOptionsAsync(srcm, smco.Sor);

                if (_shippingManagerSettings.TestMode)
                {
                    string message = "Shipping Manager - Custom Shipping Service > For Store : " + storeId + 
                        " for Method: " + srcm.PluginDescriptor.SystemName + 
                        " > Method Option Count: " + shippingMethodOptions.ShippingOptions.Count +
                        " Error Count: " + shippingMethodOptions.Errors.Count();
                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                }

                if (shippingMethodOptions.Errors.Count() == 0)
                {
                    foreach (var option in shippingMethodOptions.ShippingOptions)
                    {
                        decimal? rate = CalculateRate(smco.Smbwtr, option.Rate, requestOption.Weight);
                        if (rate.HasValue)
                            option.Rate = await _priceCalculationService.RoundPriceAsync(rate.Value * smco.Quantity);

                        var formatedOption = await shippingManagerService.FormatOptionDetails(option, smco);
                        option.Name = formatedOption.Name;
                        option.ShippingRateComputationMethodSystemName = srcm.PluginDescriptor.SystemName;
                        option.Description = formatedOption.Description;
                        option.TransitDays = formatedOption.TransitDays;
                        option.DisplayOrder = formatedOption.DisplayOrder;
                    }
                }
                else
                {
                    foreach (var option in shippingMethodOptions.ShippingOptions)
                        option.Rate = 0;

                    var product = await _productService.GetProductByIdAsync(requestOption.ProductId);
                    if (product != null)
                    {
                        for (int i = 0; i < shippingMethodOptions.Errors.Count(); i++)
                            shippingMethodOptions.Errors[i] += " for Product " + product.Name;
                    }
                }

                requestOption.Gsor = await ShippingOptionCombineResponsesAsync(requestOption.Gsor, shippingMethodOptions, storeId);

                foreach (var so in smco.Sor)
                    so.Items.FirstOrDefault().ShoppingCartItem.Quantity = smco.Quantity;
            }
            else
            {
                // request the rate for a package

                var packageShippingMethodOptions = await GetShippingMethodOptionsAsync(srcm, smco.Sor);

                if (_shippingManagerSettings.TestMode)
                {
                    string message = "Shipping Manager - Custom Shipping Service > For Store : " + storeId +
                        " for Method: " + srcm.PluginDescriptor.SystemName +
                        " > Shipping Method Option for Package Count: " + packageShippingMethodOptions.ShippingOptions.Count +
                        " Error Count: " + packageShippingMethodOptions.Errors.Count();
                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                }

                if (packageShippingMethodOptions.Errors.Count() == 0)
                {
                    foreach (var option in packageShippingMethodOptions.ShippingOptions)
                    {
                        decimal? rate = CalculateRate(smco.Smbwtr, option.Rate, requestOption.Weight);
                        if (rate.HasValue)
                            option.Rate = await _priceCalculationService.RoundPriceAsync(rate.Value * smco.Quantity);

                        var formatedOption = await shippingManagerService.FormatOptionDetails(option, smco);
                        option.Name = formatedOption.Name;
                        option.ShippingRateComputationMethodSystemName = srcm.PluginDescriptor.SystemName;
                        option.Description = formatedOption.Description;
                        option.TransitDays = formatedOption.TransitDays;
                        option.DisplayOrder = formatedOption.DisplayOrder;
                    }
                }
                else
                {
                    foreach (var option in packageShippingMethodOptions.ShippingOptions)
                        option.Rate = 0;

                    var product = await _productService.GetProductByIdAsync(requestOption.ProductId);
                    if (product != null)
                    {
                        for (int i = 0; i < packageShippingMethodOptions.Errors.Count(); i++)
                            packageShippingMethodOptions.Errors[i] += " for Product " + product.Name;
                    }
                }

                requestOption.Gsor = await ShippingOptionCombineResponsesAsync(requestOption.Gsor, packageShippingMethodOptions, storeId);
            }
        }

        /// <summary>
        /// Get both the single and package options for External Shipping Computation Methods request list organise by warehouse
        /// </summary>
        /// <param name="srcm">Shipping Rate Computation Method</param>
        /// <param name="smco">Shipping Manager Calculation Option</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping option response
        /// </returns> 
        public virtual async Task GetShippingCalculationMethodResponsesForWarehouseAsync(IShippingRateComputationMethod srcm,
            ShippingManagerCalculationRequestOption requestOption, ShippingManagerCalculationOption smco, int storeId, bool checkforShipSeperately = true)
        {
            MeasureWeight usedMeasureWeight = await GatewayMeasureWeightAsync();
            var requestSubtotal = new ShippingManagerCalculationRequestOption();
            GetShippingOptionResponse packageShippingMethodOptions = null;

            var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();

            var shipSeperatley = smco.Sor.Where(i => i.Items.Any(s => s.OverriddenQuantity != null));
            var shipTogether = smco.Sor.Where(i => i.Items.Any(s => s.OverriddenQuantity == null));

            foreach (var request in smco.Sor)
            {
                bool shipItemSeperatey = request.Items.Any(i => i.OverriddenQuantity != null);

                if (shipItemSeperatey && smco.Sor.Count() > 1)
                {
                    // request the rate for a package

                    packageShippingMethodOptions = await srcm.GetShippingOptionsAsync(request);

                    if (_shippingManagerSettings.TestMode)
                    {
                        string message = "Shipping Manager - Custom Shipping Service > For Store : " + storeId +
                            " for Method: " + srcm.PluginDescriptor.SystemName +
                            " > Shipping Method Option for Package Count: " + packageShippingMethodOptions.ShippingOptions.Count +
                            " Error Count: " + packageShippingMethodOptions.Errors.Count();
                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }

                    if (packageShippingMethodOptions.Errors.Count() == 0)
                    {
                        foreach (var option in packageShippingMethodOptions.ShippingOptions)
                        {
                            decimal? rate = CalculateRate(smco.Smbwtr, option.Rate, requestOption.Weight);
                            if (rate.HasValue)
                                option.Rate = await _priceCalculationService.RoundPriceAsync(rate.Value);

                            var formatedOption = await shippingManagerService.FormatOptionDetails(option, smco);
                            option.Name = formatedOption.Name;
                            option.ShippingRateComputationMethodSystemName = srcm.PluginDescriptor.SystemName;
                            option.Description = formatedOption.Description;
                            option.TransitDays = formatedOption.TransitDays;
                            option.DisplayOrder = formatedOption.DisplayOrder;
                        }
                    }
                    else
                    {
                        foreach (var option in packageShippingMethodOptions.ShippingOptions)
                            option.Rate = 0;

                        string warehouseName = "No Warehouse";
                        var warehouse = await GetWarehouseByIdAsync(requestOption.WarehouseId);
                        if (warehouse != null)
                            warehouseName = warehouse.Name;

                        for (int i = 0; i < packageShippingMethodOptions.Errors.Count(); i++)
                            packageShippingMethodOptions.Errors[i] += " for warehouse: " + warehouseName;
                    }

                    requestSubtotal.Gsor = await ShippingOptionCombineResponsesAsync(requestSubtotal.Gsor, packageShippingMethodOptions, storeId);

                }
                else
                { 

                    // request the rate for a package

                    packageShippingMethodOptions = await srcm.GetShippingOptionsAsync(request);

                    if (_shippingManagerSettings.TestMode)
                    {
                        string message = "Shipping Manager - Custom Shipping Service > For Store : " + storeId +
                            " for Method: " + srcm.PluginDescriptor.SystemName +
                            " > Shipping Method Option for Package Count: " + packageShippingMethodOptions.ShippingOptions.Count +
                            " Error Count: " + packageShippingMethodOptions.Errors.Count();
                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }

                    if (packageShippingMethodOptions.Errors.Count() == 0)
                    {
                        foreach (var option in packageShippingMethodOptions.ShippingOptions)
                        {
                            decimal? rate = CalculateRate(smco.Smbwtr, option.Rate, requestOption.Weight);
                            if (rate.HasValue)
                                option.Rate = await _priceCalculationService.RoundPriceAsync(rate.Value * smco.Quantity);

                            var formatedOption = await shippingManagerService.FormatOptionDetails(option, smco);
                            option.Name = formatedOption.Name;
                            option.ShippingRateComputationMethodSystemName = srcm.PluginDescriptor.SystemName;
                            option.Description = formatedOption.Description;
                            option.TransitDays = formatedOption.TransitDays;
                            option.DisplayOrder = formatedOption.DisplayOrder;
                        }
                    }
                    else
                    {
                        foreach (var option in packageShippingMethodOptions.ShippingOptions)
                            option.Rate = 0;

                        string warehouseName = "No Warehouse";
                        var warehouse = await GetWarehouseByIdAsync(requestOption.WarehouseId);
                        if (warehouse != null)
                            warehouseName = warehouse.Name;

                        for (int i = 0; i < packageShippingMethodOptions.Errors.Count(); i++)
                            packageShippingMethodOptions.Errors[i] += " for warehouse: " + warehouseName;
                    }

                    requestOption.Gsor = await ShippingOptionCombineResponsesAsync(requestOption.Gsor, packageShippingMethodOptions, storeId);

                }
            }
            
            if (requestSubtotal.Gsor.ShippingOptions.Count() > 0)
                requestOption.Gsor = await ShippingOptionCombineResponsesAsync(requestOption.Gsor, requestSubtotal.Gsor, storeId);

        }

        /// <summary>
        /// Gets the shipping option response for a Shipping Calcualtion Method for the specified requests
        /// </summary>
        /// <param name="srcm">Shipping Rate Computation Method</param>
        /// <param name="shippingOptionRequests">Shipping Option Request List</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping option response
        /// </returns>         
        private async Task<GetShippingOptionResponse> GetShippingMethodOptionsAsync(IShippingRateComputationMethod srcm, IList<GetShippingOptionRequest> shippingOptionRequests)
        {
            var result = new GetShippingOptionResponse();

            if (srcm == null || shippingOptionRequests.Count() == 0)
                return result;

            //request shipping options (separately for each package-request)
            IList<ShippingOption> srcmShippingOptions = null;
            foreach (var shippingOptionRequest in shippingOptionRequests)
            {
                var getShippingOptionResponse = await srcm.GetShippingOptionsAsync(shippingOptionRequest);

                if (getShippingOptionResponse.Success)
                {
                    //success
                    if (srcmShippingOptions == null)
                    {
                        //first shipping option request
                        srcmShippingOptions = getShippingOptionResponse.ShippingOptions;
                    }
                    else
                    {
                        //get shipping options which already exist for prior requested packages for this scrm (i.e. common options)
                        srcmShippingOptions = srcmShippingOptions
                            .Where(existingso => getShippingOptionResponse.ShippingOptions.Any(newso => newso.Name == existingso.Name))
                            .ToList();

                        //and sum the rates
                        foreach (var existingso in srcmShippingOptions)
                        {
                            existingso.Rate += getShippingOptionResponse
                                .ShippingOptions
                                .First(newso => newso.Name == existingso.Name)
                                .Rate;
                        }
                    }
                }
                else
                {
                    //errors
                    foreach (var error in getShippingOptionResponse.Errors)
                    {
                        result.AddError(error);
                        await _logger.WarningAsync($"Shipping ({srcm.PluginDescriptor.FriendlyName}). {error}");
                    }
                    //clear the shipping options in this case
                    srcmShippingOptions = new List<ShippingOption>();
                    break;
                }
            }

            //add this scrm's options to the result
            if (srcmShippingOptions != null)
            {

                foreach (var so in srcmShippingOptions)
                {
                    //set system name if not set yet
                    if (string.IsNullOrEmpty(so.ShippingRateComputationMethodSystemName))
                        so.ShippingRateComputationMethodSystemName = srcm.PluginDescriptor.SystemName;
                    if (_shoppingCartSettings.RoundPricesDuringCalculation)
                        so.Rate = await _priceCalculationService.RoundPriceAsync(so.Rate);
                    result.ShippingOptions.Add(so);
                }

                if (_shippingSettings.ReturnValidOptionsIfThereAreAny)
                {
                    //return valid options if there are any (no matter of the errors returned by other shipping rate computation methods).
                    if (result.ShippingOptions.Any() && result.Errors.Any())
                        result.Errors.Clear();
                }
            }

            //no shipping options loaded
            if (!result.ShippingOptions.Any() && !result.Errors.Any())
                result.Errors.Add(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.ShippingOptionCouldNotbeLoaded"));

            return result;

        }

        /// <summary>
        /// Calculate the rate by weight record options
        /// </summary>
        /// <param name="shippingByWeightByTotalRecord">ShippingManagerByWeightByTotal</param>
        /// <param name="rate">Quoted rate</param>/// 
        /// <param name="weight">weight</param>
        /// <returns>The calculated rate</returns>
        private decimal? CalculateRate(ShippingManagerByWeightByTotal shippingByWeightByTotalRecord, decimal rate, decimal weight)
        {
            // Formula: {[additional fixed cost] + ([order total weight] - [lower weight limit]) * [rate per weight unit]} * [charge percentage]

            if (shippingByWeightByTotalRecord == null)
            {
                if (_shippingManagerSettings.LimitMethodsToCreated)
                    return null;

                return decimal.Zero;
            }

            //additional fixed cost
            var shippingTotal = shippingByWeightByTotalRecord.AdditionalFixedCost + rate;

            //charge amount per weight unit
            if (shippingByWeightByTotalRecord.RatePerWeightUnit > decimal.Zero)
            {
                var weightRate = Math.Max(weight - shippingByWeightByTotalRecord.LowerWeightLimit, decimal.Zero);
                shippingTotal += shippingByWeightByTotalRecord.RatePerWeightUnit * weightRate;
            }

            if (shippingByWeightByTotalRecord.PercentageRateOfSubtotal > decimal.Zero)
            {
                shippingTotal += Math.Round((decimal)((((float)shippingTotal) * ((float)shippingByWeightByTotalRecord.PercentageRateOfSubtotal)) / 100f), 2);
            }

            return Math.Max(shippingTotal, decimal.Zero);
        }

        #endregion

        #region Shipping.Aramex (Fastway)

        /// <summary>
        /// Get both the single and package options for Fastway Shipping Service request list organise by product
        /// </summary>
        /// <param name="srcm">Shipping Rate Computation Method</param>
        /// <param name="smco">Shipping Manager Calculation Option</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping option response
        /// </returns> 
        public virtual async Task GetFastwayResponsesForProductAsync(ShippingManagerCalculationRequestOption requestOption, ShippingManagerCalculationOption smco, int storeId)
        {
            var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();

            if (await ShipSeperately(smco))
            {
                //rather than request to the get a rate for each package - request the rate once and multiply by quantity

                foreach (var so in smco.Sor)
                    so.Items.FirstOrDefault().ShoppingCartItem.Quantity = 1;

                var shippingMethodOptions = await _fastwayService.GetShippingMethodOptionsAsync(smco.Sor);

                if (_shippingManagerSettings.TestMode)
                {
                    string message = "Shipping Manager - Custom Shipping Service > For Store : " + storeId +
                        " Fastway Shipping Method Option Count: " + shippingMethodOptions.ShippingOptions.Count +
                        " Error Count: " + shippingMethodOptions.Errors.Count();
                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                }

                if (shippingMethodOptions.Errors.Count() == 0)
                {
                    foreach (var option in shippingMethodOptions.ShippingOptions)
                    {
                        option.Rate = (await _fastwayService.CalculateRate(smco, option.Rate, requestOption.Weight)).Value;
                        option.Rate = await _priceCalculationService.RoundPriceAsync(option.Rate * smco.Quantity);

                        var formatedOption = await _fastwayService.FormatOptionDetails(option, smco);
                        option.Name = formatedOption.Name;
                        option.Description = formatedOption.Description;
                        option.TransitDays = formatedOption.TransitDays;
                        option.DisplayOrder = formatedOption.DisplayOrder;
                    }
                }
                else
                {
                    foreach (var option in shippingMethodOptions.ShippingOptions)
                        option.Rate = 0;

                    var product = await _productService.GetProductByIdAsync(requestOption.ProductId);
                    if (product != null)
                    {
                        for (int i = 0; i < shippingMethodOptions.Errors.Count(); i++)
                            shippingMethodOptions.Errors[i] += " for Product " + product.Name;
                    }
                }

                requestOption.Gsor = await ShippingOptionCombineResponsesAsync(requestOption.Gsor, shippingMethodOptions, storeId);

                foreach (var so in smco.Sor)
                    so.Items.FirstOrDefault().ShoppingCartItem.Quantity = smco.Quantity;
            }
            else
            {
                // request the rate for a package

                var packageShippingMethodOptions = await _fastwayService.GetShippingMethodOptionsAsync(smco.Sor);

                if (_shippingManagerSettings.TestMode)
                {
                    string message = "Shipping Manager - Custom Shipping Service > For Store : " + storeId +
                        " Fastway Shipping Method Option for Package Count: " + packageShippingMethodOptions.ShippingOptions.Count +
                        " Error Count: " + packageShippingMethodOptions.Errors.Count();
                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                }

                if (packageShippingMethodOptions.Errors.Count() == 0)
                {
                    foreach (var option in packageShippingMethodOptions.ShippingOptions)
                    {
                        option.Rate = (await _fastwayService.CalculateRate(smco, option.Rate, requestOption.Weight)).Value;
                        option.Rate = await _priceCalculationService.RoundPriceAsync(option.Rate * smco.Quantity);

                        var formatedOption = await _fastwayService.FormatOptionDetails(option, smco);
                        option.Name = formatedOption.Name;
                        option.Description = formatedOption.Description;
                        option.TransitDays = formatedOption.TransitDays;
                        option.DisplayOrder = formatedOption.DisplayOrder;
                    }
                }
                else
                {
                    foreach (var option in packageShippingMethodOptions.ShippingOptions)
                        option.Rate = 0;

                    var product = await _productService.GetProductByIdAsync(requestOption.ProductId);
                    if (product != null)
                    {
                        for (int i = 0; i < packageShippingMethodOptions.Errors.Count(); i++)
                            packageShippingMethodOptions.Errors[i] += " for Product " + product.Name;
                    }
                }

                requestOption.Gsor = await ShippingOptionCombineResponsesAsync(requestOption.Gsor, packageShippingMethodOptions, storeId);
            }
        }

        /// <summary>
        /// Get both the single and package options for Fastway Shipping Service request list organise by warehouse
        /// </summary>
        /// <param name="warehouse">Shipping Manager Calculation Warehouse Option</param>
        /// <param name="smco">Shipping Manager Calculation Option</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping option response
        /// </returns> 
        public virtual async Task GetFastwayResponsesForWarehouseAsync(ShippingManagerCalculationRequestOption requestOption, ShippingManagerCalculationOption smco, int storeId)
        {
            MeasureWeight usedMeasureWeight = await GatewayMeasureWeightAsync();
            var requestSubtotal = new ShippingManagerCalculationRequestOption();
            GetShippingOptionResponse packageShippingMethodOptions = null;

            var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();

            var shipSeperatley = smco.Sor.Where(i => i.Items.Any(s => s.OverriddenQuantity != null));
            var shipTogether = smco.Sor.Where(i => i.Items.Any(s => s.OverriddenQuantity == null));

            foreach (var request in smco.Sor)
            {
                bool shipItemSeperatey = request.Items.Any(i => i.OverriddenQuantity != null);

                if (shipItemSeperatey && smco.Sor.Count() > 1)
                {
                    // request the rate for a package

                    packageShippingMethodOptions = await _fastwayService.GetShippingOptionsAsync(request);

                    if (_shippingManagerSettings.TestMode)
                    {
                        string message = "Shipping Manager - Custom Shipping Service > For Store : " + storeId +
                            " Fastway Shipping Method Option for Package Count: " + packageShippingMethodOptions.ShippingOptions.Count +
                            " Error Count: " + packageShippingMethodOptions.Errors.Count();
                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }

                    if (packageShippingMethodOptions.Errors.Count() == 0)
                    {
                        foreach (var option in packageShippingMethodOptions.ShippingOptions)
                        {
                            decimal? rate = await _fastwayService.CalculateRate(smco, option.Rate, requestOption.Weight);
                            if (rate.HasValue)
                                option.Rate = await _priceCalculationService.RoundPriceAsync(rate.Value * smco.Quantity);

                            var formatedOption = await _fastwayService.FormatOptionDetails(option, smco);
                            option.Name = formatedOption.Name;
                            option.Description = formatedOption.Description;
                            option.TransitDays = formatedOption.TransitDays;
                            option.DisplayOrder = formatedOption.DisplayOrder;
                        }
                    }
                    else
                    {
                        foreach (var option in packageShippingMethodOptions.ShippingOptions)
                            option.Rate = 0;

                        string warehouseName = "No Warehouse";
                        var warehouse = await GetWarehouseByIdAsync(requestOption.WarehouseId);
                        if (warehouse != null)
                            warehouseName = warehouse.Name;

                        for (int i = 0; i < packageShippingMethodOptions.Errors.Count(); i++)
                            packageShippingMethodOptions.Errors[i] += " for warehouse: " + warehouseName;
                    }

                    requestSubtotal.Gsor = await ShippingOptionCombineResponsesAsync(requestSubtotal.Gsor, packageShippingMethodOptions, storeId);

                }
                else
                {
                    // request the rate for a package

                    packageShippingMethodOptions = await _fastwayService.GetShippingOptionsAsync(request);

                    if (_shippingManagerSettings.TestMode)
                    {
                        string message = "Shipping Manager - Custom Shipping Service > For Store : " + storeId +
                            " Fastway Shipping Method Option for Package Count: " + packageShippingMethodOptions.ShippingOptions.Count +
                            " Error Count: " + packageShippingMethodOptions.Errors.Count();
                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }

                    if (packageShippingMethodOptions.Errors.Count() == 0)
                    {
                        foreach (var option in packageShippingMethodOptions.ShippingOptions)
                        {
                            decimal? rate = await _fastwayService.CalculateRate(smco, option.Rate, requestOption.Weight);
                            if (rate.HasValue)
                                option.Rate = await _priceCalculationService.RoundPriceAsync(rate.Value * smco.Quantity);

                            var formatedOption = await _fastwayService.FormatOptionDetails(option, smco);
                            option.Name = formatedOption.Name;
                            option.Description = formatedOption.Description;
                            option.TransitDays = formatedOption.TransitDays;
                            option.DisplayOrder = formatedOption.DisplayOrder;
                        }
                    }
                    else
                    {
                        foreach (var option in packageShippingMethodOptions.ShippingOptions)
                            option.Rate = 0;

                        string warehouseName = "No Warehouse";
                        var warehouse = await GetWarehouseByIdAsync(requestOption.WarehouseId);
                        if (warehouse != null)
                            warehouseName = warehouse.Name;

                        for (int i = 0; i < packageShippingMethodOptions.Errors.Count(); i++)
                            packageShippingMethodOptions.Errors[i] += " for warehouse: " + warehouseName;
                    }

                    requestOption.Gsor = await ShippingOptionCombineResponsesAsync(requestOption.Gsor, packageShippingMethodOptions, storeId);

                }
            }

            if (requestSubtotal.Gsor.ShippingOptions.Count() > 0)
                requestOption.Gsor = await ShippingOptionCombineResponsesAsync(requestOption.Gsor, requestSubtotal.Gsor, storeId);
        }

        #endregion

        #region Shipping.Sendcloud

        /// <summary>
        /// Get both the single and package options for Fastway Shipping Service request list organise by product
        /// </summary>
        /// <param name="srcm">Shipping Rate Computation Method</param>
        /// <param name="smco">Shipping Manager Calculation Option</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping option response
        /// </returns> 
        public virtual async Task GetSendcloudResponsesForProductAsync(ShippingManagerCalculationRequestOption requestOption, int storeId)
        {
            var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();

            // request the rate for a package
            var sendcloudService = EngineContext.Current.Resolve<ISendcloudService>();
            var packageShippingMethodOptions = await sendcloudService.GetShippingMethodOptionsAsync(requestOption.Smco.FirstOrDefault());

            if (_shippingManagerSettings.TestMode)
            {
                string message = "Shipping Manager - Custom Shipping Service > For Store : " + storeId +
                    " Sendcloud Shipping Method Option for Package Count: " + packageShippingMethodOptions.ShippingOptions.Count +
                    " Error Count: " + packageShippingMethodOptions.Errors.Count();
                await _logger.InsertLogAsync(LogLevel.Debug, message);
            }

            if (packageShippingMethodOptions.Errors.Count() == 0)
            {
                foreach (var option in packageShippingMethodOptions.ShippingOptions)
                {
                    // Nothing to do 
                }
            }
            else
            {
                foreach (var option in packageShippingMethodOptions.ShippingOptions)
                    option.Rate = 0;

                var product = await _productService.GetProductByIdAsync(requestOption.ProductId);
                if (product != null)
                {
                    for (int i = 0; i < packageShippingMethodOptions.Errors.Count(); i++)
                        packageShippingMethodOptions.Errors[i] += " for Product " + product.Name;
                }
            }

            requestOption.Gsor = packageShippingMethodOptions;

        }

        #endregion

    }
}
