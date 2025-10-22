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

        this.pagerElem.find("a.page-link").on("click", (event) => {
            this._handlePageClick();
            event.preventDefault();
        });
    }

    // Handles a user click on a page item.
    _handlePageClick() {
        let pageIndex = this.pagerElem.data("page");
        this.pagerElem.closest(".rs-pager").find("input:hidden:first").val(pageIndex);

        if (this.options.submitOnClick) {
            this.pagerElem.closest("form").trigger("submit");
        } else {
            this.pagerElem.trigger(RazorPager.PAGE_CLICK_EVENT, pageIndex);
        }
    }

    // Sets the current page index to zero.
    reset(opt_submit) {
        this.pagerElem.find("input:hidden:first").val(0);

        if (opt_submit) {
            this.pagerElem.closest("form").trigger("submit");
        }
    }
}
