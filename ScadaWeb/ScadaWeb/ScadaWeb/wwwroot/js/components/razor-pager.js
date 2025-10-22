// Represents a component for managing pagination, working in conjunction with PagerTagHelper.
// Depends on jquery
class RazorPager {
    // Notifies that a page item has been clicked.
    static PAGE_CLICK_EVENT = "pageclick.rs.pager";

    // The default pager options.
    static defaultOptions = {
        submitOnClick: false // submits the form when a page is clicked
    };

    pagerElem; // jQuery object that represents the pager
    options;   // pager options

    constructor(pagerElemID, opt_options) {
        this.pagerElem = $("#" + pagerElemID);
        this.options = Object.assign({}, RazorPager.defaultOptions, opt_options);

        this.pagerElem.find("a.page-link").on("click", function () {
            Pager._handlePageClick($(this));
            return false;
        });
    }

    // Handles a user click on a page item.
    static _handlePageClick(elem) {
        let pageIndex = elem.data("page");
        elem.closest(".rs-pager").find("input:hidden:first").val(pageIndex);

        if (this.options.submitOnClick) {
            elem.closest("form").trigger("submit");
        } else {
            elem.trigger(RazorPager.PAGE_CLICK_EVENT, thisObj.pageIndex);
        }
    }

    // Sets the current page index to zero.
    reset(opt_submit) {
        this.pagerElem.find("input:hidden:first").val(0);

        if (opt_submit) {
            elem.closest("form").trigger("submit");
        }
    }

    // Binds events to all pagers of the document.
    static bindEvents() {
        $(".rs-pager a.page-link").on("click", function () {
            Pager._handlePageClick($(this));
            return false;
        });
    }
}
