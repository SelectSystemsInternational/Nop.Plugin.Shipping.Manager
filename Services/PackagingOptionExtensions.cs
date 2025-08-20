using System.Linq;
using Nop.Plugin.Shipping.Manager.Domain;

namespace Nop.Plugin.Shipping.Manager.Services
{
    public static class PackagingOptionExtensions
    {
        /// <summary>
        /// Sorts the elements of a sequence in order according to a product sorting rule
        /// </summary>
        /// <param name="packagingOptionQuery">A sequence of products to order</param>
        /// <param name="orderBy">Product sorting rule</param>
        /// <returns>An System.Linq.IOrderedQueryable`1 whose elements are sorted according to a rule.</returns>
        /// <remarks>
        /// If <paramref name="orderBy"/> is set to <c>Position</c> and passed <paramref name="productsQuery"/> is
        /// ordered sorting rule will be skipped
        /// </remarks>
        public static IQueryable<PackagingOption> OrderBy(this IQueryable<PackagingOption> packagingOptionQuery, PackagingOptionSortingEnum orderBy) 
        {
            return orderBy switch
            {
                PackagingOptionSortingEnum.NameAsc => packagingOptionQuery.OrderBy(po => po.Name),
                PackagingOptionSortingEnum.NameDesc => packagingOptionQuery.OrderByDescending(po => po.Name),
                PackagingOptionSortingEnum.PriceAsc => packagingOptionQuery.OrderBy(po => po.Price),
                PackagingOptionSortingEnum.PriceDesc => packagingOptionQuery.OrderByDescending(po => po.Price),
                PackagingOptionSortingEnum.CreatedOn => packagingOptionQuery.OrderByDescending(po => po.CreatedOnUtc),
                PackagingOptionSortingEnum.Position when packagingOptionQuery is IOrderedQueryable => packagingOptionQuery,
                    _ => packagingOptionQuery.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id)
            };
        }
    }
}