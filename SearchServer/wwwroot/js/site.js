// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

var _flips=[];

$(function(){

/*
 $(".search-control").on('input',function(){
    addSearchSuggessions(this,this.dataset['suggests']);
  });
 $(".search-control").focus(function(){
    addSearchSuggessions(this,this.dataset['suggests']);
  });
  $(".search-control").keyup(function(){
    $("#SearchSugg div").css('margin-left',-$(this).scrollLeft());
  });


 $(".search-control").blur(function(){
   removeSearchSuggession();
});
  */
 $(".search-control").autocomplete({'source':function(req,resp){
    if (req.term.length<2) resp("");
    $.getJSON($(".search-control").data("suggests")+req.term,function(data){

      $.each(data,function(i){data[i] = req.term+data[i];});
      resp(data);
    });
   }}); 


     $('.rrssb-buttons').parent().parent().children().first().click(function () {
                $('.rrssb-buttons').removeClass('tiny-format');
        })

     if (typeof($.fn.rrssb)!='undefined'){
     $('.rrssb-buttons').rrssb({
        // required:
         title: document.title,
         url: document.location.href,
     });
    }



});


(function($) {

$.fn.serializefiles = function() {
    var obj = $(this);
    /* ADD FILE TO PARAM AJAX */
    var formData = new FormData();
    $.each($(obj).find("input[type='file']"), function(i, tag) {
        $.each($(tag)[0].files, function(i, file) {
            formData.append(tag.name, file);
        });
    });
    var params = $(obj).serializeArray();
    $.each(params, function (i, val) {
        formData.append(val.name, val.value);
    });
    return formData;
};
})(jQuery);

$(function(){

    $(".lazyload").lazyload({
 data_attribute:"src"
});






     $(".sticky").each(function(i,el){
        if (el.id=="main-menu")
         $(el).stick_in_parent();
        else
         $(el).stick_in_parent({offset_top:$("#main-menu")[0].offsetHeight});
      });


        $("form[data-ajax='true']").each(function(i,el){
            $(el).submit(function (e) {
                $.post($(this).action, $(this).serialize(), function (data) {
                    var newDoc = document.open("text/html", "replace");
                    newDoc.write(data);
                    newDoc.close();
                });

                e.preventDefault();
            });

       });

        $("a[data-ajax='true']").each(function(i,el){

          $(el).click(function(e){

              if ((!el.dataset["confirm"]) || (confirm(el.dataset["confirm"])))
                  $.ajax({
                    url:el.href,
                    type:(el.dataset["method"]?el.dataset["method"]:"get"),
                    success:function (data) {
                       if (data.responseJSON)
                        data = data.responseJSON;
                       if (data.error) alert(data.error);
                       else
                      if (el.dataset["ajaxSuccess"]) eval(el.dataset["ajaxSuccess"]);
                  }

               });

                   e.preventDefault();

             });

       });


    $(function () {
        $('[data-toggle="popover"]').each(function(i,el){
         var $el = $(el);
         if ($el.data("content").startsWith("#"))
         {
          var id = this.dataset["content"];
          delete (this.dataset["content"]);
          $el.popover({
            content: function(){
              return $(id).html();
             }

          });
         }
        else $el.popover();
        });
    });

    $(function () {
        $('[data-toggle="tooltip"]').tooltip()
    });
  /*  

   $('#navbarSupportedContent').on('show.bs.collapse', function (e) {
      $('#search_place').append($("#search").children().first());

   })
    $('#navbarSupportedContent').on('hidden.bs.collapse', function (e) {
        if (e.target.id!='searchmore')
         $('#search').append($("#search_place").children().first());
   })
    */


        $('.expand-button').each(function () {
            this.dataset["expand"] = this.innerHTML;
            if (this.previousElementSibling.scrollHeight <= this.previousElementSibling.clientHeight) {
                $(this.previousElementSibling).addClass('-expanded');
                this.remove();
            }
            
        });

        $('.expand-button').on('click', function () {
            $(this.previousElementSibling).toggleClass('-expanded');
            if ($(this.previousElementSibling).hasClass('-expanded')) {
                $('.expand-button').html(this.dataset["collapse"]||"Hide");
            } else {
                $('.expand-button').html(this.dataset["expand"]);
            }
        });

});



/// update column margins
$.fn.updatecolumns = function (){
 var $elems = this;
 if ($elems.width()>1)
 {
  $elems.parent().css('position','relative');
  var n = parseInt($elems.parent().width()/($elems.width()-1));
  var s = [];
  for (q=0;q<n;q++) s[q] = 0;
  var i=0;
  $elems.each(function(){
    var $this = $(this);
    if (i<n)
      $this.css('margin-top','0px');
    else
     {
      $this.css('margin-top',(s[i%n]-$this.position().top)+"px");
     }
    s[i%n]+=$this.children().first().outerHeight();
    i++;
  });
  $(window).trigger('scroll');
 }
}

// dir = 1 - down, -1 - up;
function scroll_window(dir){
  var H = window.innerHeight*0.95-$("#main-menu").outerHeight();
  N = 10;
  for (var q=0;q<N;q++){
    window.setTimeout(function(){ 
   window.scrollBy(0,dir*H/N)},500*q/N);
 }
}



/**** VUE *****/

Vue.component('users-list', {
 props: {
  users:Array,
  n:{
    default:10,
    type: Number
  }
 },
 methods:{
   maketooltip: function(e){
    var el = e.target;
    el.dataset["content"] = $(el.nextSibling).html();
  }
 },
 mounted:function(){
    $(this.$el).find($('[data-toggle="tooltip"]')).tooltip({
     title: function(){
      return this.nextSibling.innerHTML;
       }
   })
 },
 template:`<span>
            <a v-for="user in users.slice(0,n)" class='thumb-xxs' :href="'/Users/Id/'+user.Id" :title="user.Name">
                <img :src="user.Image" class="rounded-circle"/>
            </a>
            <button v-if="users.length>n" data-toggle="tooltip" data-placement="bottom" v-on: data-html="true">+{{users.length-n}} more</button><div class='d-none'>
             <a v-for="user in users.slice(1)" class='thumb-xxs' :href="user.Link" :title="user.Name">
                <img :src="user.Image" class="rounded-circle"/>
             </a>
            </div>
</span>`
});

Vue.component('titled-users-list', {
 props: ["users"],
 template:`<div  class="row" ><div v-for="user in users.slice(0,10)" class="col thumb-xs">
            <a :href="'/Users/Id/'+user.Id" :title="user.Name">
                <img :src="user.Image" class="rounded-circle"/>
            </a> 
     <div class="title"><a :href="user.Link" :title="user.Name">{{user.Name}}</a></div>  
</div></div>`
});


Vue.component('titled-groups-list', {
 props: ["groups"],
 template:`<div class='row'><div v-for="group in groups.slice(0,10)" class="col thumb-xs">
            <a :href="'/Groups/Id/'+group.Id" :title="group.Name">
                <img :src="group.Image" class="rounded-circle"/>
            </a> 
     <div class="title"><a :href="group.Link" :title="group.Name">{{group.Name}}</a></div>  
</div></div>`
});



Vue.component('tags-list',{
        props: {
           items: Array,
           hints: Array,
           horz: {
            default: false,
            type: Boolean
          }
        },
        data: function () {
            return { newtag : '', 
            id: new Date().getTime() };
        },
        template: `<div><ul :class="horz?'nav':'list-group'"><li :class="horz?'nav-item':'list-group-item d-flex justify-content-between align-items-center'" v-for="(item, index) in items">{{item}}<span><a href="" v-if="(index>0) && (!horz)" v-on:click.prevent="up_tag(index)"><i class="fas fa-chevron-circle-up"></i></a><a href="" v-if="(index<items.length-1) && (!horz)" v-on:click.prevent="down_tag(index)"><i class="fas fa-chevron-circle-down"></i></a><a class="mr-3" href="" v-on:click.prevent="delete_tag(index)"><i class="fas fa-minus-circle"></i></a></span></li></ul>
           <div><input type="text" :list="'tags_list_hints'+id" v-model="newtag"/><button type="button" v-on:click.prevent="add_tag(newtag);newtag=''">Add</button></div>
         <datalist :id="'tags_list_hints'+id">
 	 <select>
		<option v-for="item in hints" :value="item">{{item}}</option>
 	 </select>
        </datalist></div>
        `,
        methods: {
            delete_tag(i) {
                this.items.splice(i,1);
            },
            add_tag(s) {
                this.items.push(s);
            },
            up_tag(i) {
                if ((i > 0) && (this.items.length > 1)) {
                    var t = this.items[i - 1];
                    this.items[i - 1] = this.items[i];
                    this.items[i] = t;
                    this.items = this.items.slice(0);
                }
            },
            down_tag(i) {
                if ((i < this.items.length - 1) && (this.items.length > 1)) {
                    var t = this.items[i + 1];
                    this.items[i + 1] = this.items[i];
                    this.items[i] = t;
                    this.items = this.items.slice(0);
                }

            }
        }
    });




var VueBus = new Vue({});

function unique(arr){
return arr.filter(function (item, pos) {
                    return arr.indexOf(item) == pos;
                });
}


