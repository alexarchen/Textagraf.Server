/* Live Textagraf Document JS library
/* Copyright 2019
/* AlexArchen
/* textagraf.com
// LICENCE: MIT
*/

if (typeof txtGLiveThumb !== "function"){	


 function txtGLiveThumb(elem, options) {

        // Allow instantiation without `new` keyword
        if (!(this instanceof txtGLiveThumb)) {
            return new txtGLiveThumb(elem, options);
        }

        this.images = [];
        this.images[0] = previmg = this.baseimg = elem.firstElementChild;
        this.elem = elem;
        var self = this;

        this.loadfunc = function(el,n){options.loadfunc(self,el,n)};
        this.elem.addEventListener('mousedown',function(e) {self._mousedown(e)});
        this.elem.addEventListener('mouseup',function(e){self._mouseup(e)});
        this.elem.addEventListener('mousemove',function(e){self._mousemove(e);});
        this.elem.addEventListener('mouseleave',function(e){self._mouseup(e);});
        this.elem.addEventListener('touchstart',function(e) {self._mousedown(e)});
        this.elem.addEventListener('touchend',function(e){self._mouseup(e)});
        this.elem.addEventListener('touchmove',function(e){self._mousemove(e);});
//        $(this.elem).on('click',function(e){document.location.href=elem.dataset["href"]});
        this.elem.addEventListener('resize',function(){
          self.resize(); return false;});
        this.n = elem.dataset["n"]||options.n||1;
        for (var i=1;i<this.n;i++)
         {
          this.images[i] = new Image();
          this.images[i].style.position ='absolute';
          this.images[i].style.width='100%';
          this.elem.insertBefore(this.images[i],previmg);
          previmg = this.images[i];
          }

        this.images.push(null);
//box-shadow: 5px 0px 20px 10px rgba(0,0,0,0.3);
//        roll = $("<div style='pointer-events: none; position:absolute; top:0px; bottom: 0px; left:0px; width:100%; transform: matrix(0,0,0,1,0,0);  background: linear-gradient(to right, rgb(255, 255, 255) 0%, rgb(220, 220, 220) 66%, rgb(160, 160, 160) 100%); border: 1px solid rgb(180,180,180); '</div>");
        this.roll = document.createElement("DIV");
        this.roll.setAttribute("style","box-shadow: 5px 0px 20px 10px rgba(0,0,0,0.3); pointer-events: none; position:absolute; top:0px; bottom: 0px; left:0px; width:100%; transform: matrix(0,0,0,1,0,0);  background: linear-gradient(to right, rgb(255, 255, 255) 0%, rgb(220, 220, 220) 66%, rgb(160, 160, 160) 100%); border: 1px solid rgb(180,180,180);");
        this.roll.addEventListener('transitionend',function(e){self.preparenext();});
        this.elem.appendChild(this.roll);
        this.currimg = this.images[0];
        this.previmg = null;
        this.nextimg = this.images[1];
        this.currPage = 0;
        this.currimg.style.transform = "translateZ(0)";
        if (this.nextimg)
         this.nextimg.style.transform = "translateZ(0)";

        if (this.baseimg.naturalWidth*this.baseimg.naturalHeight>0)
          {
            self.resize();
          }
        else
         this.baseimg.onload = function(){self.baseimg.onload=null; self.resize()};

         this.loadfunc(this.baseimg,1);
    }


    txtGLiveThumb.prototype = {
        constructor: txtGLiveThumb,
        instance: function () {
            return this;
        },
        loadfunc: function () { },
        nextpage: function () {
              dt = 10000/Math.max(50,this.velocity/8);
              this.currimg.style.transition = dt+"ms ease-out";
              this.roll.style.transition = dt+"ms ease-out";
              this.currimg.style.clip="rect(auto,0px,auto,auto)";
              this.roll.style.transform= "matrix(0.01,0,0,1,"+(/*-this.elem.clientWidth*/-this.roll.clientWidth/2)+",0)"; 

//              $(this.roll).css("transform","matrix(0.001,0,0,1,"+(-this.elem.clientWidth-this.roll.clientWidth/2)+",0)"); 
//              $(this.roll).css("width","0px"); 
//              $(this.roll).css("right",this.elem.clientWidth+"px"); 
//              $(this.roll).css("box-shadow","5px 0px 0px 0px rgba(0,0,0,0)");
//              setTimeout(this.preparenext,dt);
              this.currPage++;
              if (this.previmg) this.previmg.style.transform="none";
              this.previmg = this.currimg;
              this.currimg = this.images[this.currPage];
              this.nextimg = this.images[this.currPage+1];
              if (this.nextimg)
              this.nextimg.style.transform = "translateZ(0)";

              this.preload();
        },
        prevpage: function(){
              dt = 10000/Math.max(50,-this.velocity/8);
              this.previmg.style.transition = dt+"ms ease-out";
              this.roll.style.transition= dt+"ms ease-out";
              this.previmg.style.clip="rect(auto,"+this.elem.clientWidth+"px,auto,auto)";
              this.roll.style.transform="matrix(0.0001,0,0,1,"+(this.roll.clientWidth/2)+",0)"; 
//              $(this.roll).css("width","0px"); 
//              $(this.roll).css("right","0px"); 
//              $(this.roll).css("box-shadow","5px 0px 20px 10px rgba(0,0,0,0)");
//             setTimeout(this.preparenext,dt);
              if (this.nextimg) this.nextimg.style.transform="none";
              this.currPage--;
              this.nextimg = this.currimg;
              this.currimg = this.images[this.currPage];
              this.previmg =  this.currPage>0?this.images[this.currPage-1]:null;
              if (this.previmg)
               this.previmg.style.transform="translateZ(0)";
        },
        resize: function(){
          var w = this.baseimg.naturalWidth;
          var h = this.baseimg.naturalHeight;
          var pw = this.elem.parentElement.clientWidth;
          var ph = this.elem.parentElement.clientHeight;
/*
          var height = "100%";
          var width = "100%";
          if (pw/ph>w/h){
               width = w/h/(pw/ph)*100+"%";
           }
          else
           {
             height = (pw/ph)/(w/h)*100+"%";
           }
*/
          var height = ph;
          var width = pw;

          if (pw/ph>w/h){
               width = w/h*ph;
           }
          else
           {
             height = pw/(w/h);
           }

           this.elem.style.width = width+"px";
           this.elem.style.height = height+"px";
           this.baseimg.style.position = "absolute";
           this.baseimg.style.width="100%";

/*
           if (this.canvas) $(this.canvas).remove();
           if ((w>h) && (w>500)) { h = 500*h/w; w = 500;}
            else
           if ((w<h) && (h>500)) { w = 500*w/h; h = 500;}

           this.canvas = $("<canvas  style='display: none; ' height="+(h * 2)+" width="+ (w * 2)+"</canvas>");
           $(this.elem).append(this.canvas);
           this.canvas = this.canvas[0];
*/
           // reload images exept 1-st
           for (q=1;q<Math.min(this.currPage+2,this.n);q++)
            {
              if (!this.images[q].src)  this.loadfunc(this.images[q], q+1);
            }
            var evt = window.document.createEvent('UIEvents'); 
            evt.initUIEvent('resize', true, false, window, 0); 
            window.dispatchEvent(evt);
        },
        preload: function () {
            if ((this.currPage < this.n - 1) && (!this.images[this.currPage+1].src))
                this.loadfunc(this.images[this.currPage+1], this.currPage + 2);
        },
        pressed: false,
        clicked: false,
        movednext: false,
        startX: 0,
        startY: 0,
        dy:0,
        dx:0,
        wasXshift:false,
        wasMove:false,
        velocity:0,
        lasttimestamp:0,
        _mousedown: function(e){
         if (e.touches)
           {
//            e.offsetX = e.touches[0].clientX - e.target.getBoundingClientRect().x;
            e.offsetX = e.touches[0].pageX - e.target.getBoundingClientRect().left;
            this.startY = e.touches[0].pageY;
           }
         this.wasMove = false;
         this.clicked = true;
         if (e.target.tagName=="IMG")
         {
          if (((e.offsetX<this.elem.clientWidth/3) && (this.currPage>0)) || ((e.offsetX>2*this.elem.clientWidth/3) && (this.currPage<this.n-1)))
          {
           this.pressed = true;
           this.wasXshift = false;
           this.startX = this.prevX = e.offsetX;

           if (e.offsetX<this.elem.clientWidth/3) this.prepareprev();
             else this.preparenext();

           if (!e.touches)
            e.preventDefault();
          }
         }
        },
        _mousemove: function (e){
         this.clicked = false;
         if ((this.pressed) && (e.target.tagName=="IMG"))
          {
           this.wasMove = true;
           if (e.touches)
           {
//            e.offsetX = e.touches[0].clientX - e.target.getBoundingClientRect().x;
            e.offsetX = e.touches[0].pageX - e.target.getBoundingClientRect().left;
            if ((!this.wasXshift) && (Math.abs(this.startY - e.touches[0].pageY))>10)
             {
               this.pressed = false;
               return;
             }
           }
            prevdx =  this.dx;
            this.dx = this.startX - e.offsetX;

           if (Math.abs(this.dx)>10) this.wasXshift = true;

           if (this.wasXshift){

             this.velocity = 1000*((this.startX - e.offsetX)-prevdx)/(e.timeStamp-this.lasttimestamp);
             this.lasttimestamp = e.timeStamp;
             this.drawshift(this.dx,this.startX>this.elem.clientWidth/2);

             e.preventDefault();

            }
          }
           if (!e.touches)
         e.preventDefault();

        },
        _mouseup: function (e){

         if (this.pressed){



         if (this.dx>0){
           // next page
           if (this.dx+this.velocity/4>this.elem.clientWidth*0.6){
             this.nextpage();
           }
           else this.clear();
         }
         else{
           // prev page
           if (-this.dx-this.velocity/4>this.elem.clientWidth*0.6){
             this.prevpage();
           }
           else this.clear();
         }
         this.pressed = false;

           if (!e.touches)
         e.preventDefault();

         }
          else{

           if (this.clicked) 
             top.document.location.href=this.elem.dataset["href"];

          }
         this.clicked = false;
        },
        clear: function(){

            this.roll.style.transition="600ms ease-in-out";
            if (this.movednext)
             {
              this.currimg.style.transition="600ms ease-in-out";
              this.currimg.style.clip= "rect(auto,"+this.currimg.clientWidth+"px,auto,auto)";
/*
              $(this.roll).css("width","0px"); 
              $(this.roll).css("right","0px"); 
*/
              this.roll.style.transform="matrix(0.001,0,0,1,"+(this.roll.clientWidth/2)+",0)"; 
//              $(this.roll).css("box-shadow","5px 0px 20px 10px rgba(0,0,0,0)");
             }
            else{
              this.previmg.style.transition="600ms ease-in-out";
              this.previmg.style.clip= "rect(auto,0px,auto,auto)";
//              $(this.roll).css("box-shadow","5px 0px 0px 0px rgba(0,0,0,0)");
//              $(this.roll).css("right",this.elem.clientWidth+"px"); 
              this.roll.style.transform="matrix(0.3,0,0,1,"+(-0.65*this.roll.clientWidth)+",0)"; 

            }
            //setTimeout(this.preparenext,650);
        },
        preparenext: function(){
              this.currimg.style.transition="none";
              this.roll.style.transition="none";

/*
              $(this.roll).css("width","0px"); 
              $(this.roll).css("right","-20px"); 
*/
              this.roll.style.transform="matrix(0,0,0,1,0,0)"; 
              this.movednext = true;
        },
        prepareprev: function(){
              if (this.previmg) this.previmg.style.transition="none";
              this.roll.style.transition="none";

/*
              $(this.roll).css("width","0px"); 
              $(this.roll).css("right",this.elem.clientWidth+"px"); 
*/
              this.roll.style.transform="matrix(0,0,0,1,0,0)";//+(-this.roll.clientWidth)+",0)"); 
              this.movednext = false;
        },
        drawshift:function(dx,next){
           if ((dx>0) && (next)){
             // draw next page
              dx = Math.min(this.elem.clientWidth,dx);
              this.currimg.style.clip= "rect(auto,"+(this.elem.clientWidth-dx)+"px,auto,auto)";
              this.roll.style.transform="matrix("+(dx/2/this.roll.clientWidth)+",0,0,1,"+(-dx+this.roll.clientWidth/2)+",0)"; 
//              $(this.roll).css("width",dx/2+"px"); 
//              $(this.roll).css("box-shadow","5px 0px 20px 10px rgba(0,0,0,"+Math.min(dx/this.roll.clientWidth,0.3)+")");
           }
          else if ((dx<0) && (!next))
          {
             // draw prev page
              dx = -dx;
              dx = Math.min(this.elem.clientWidth,dx);
              this.previmg.style.clip= "rect(auto,"+dx+"px,auto,auto)";

              if (dx<this.elem.clientWidth/4)
               {
                w = dx;
                this.roll.style.transform="matrix("+(w/this.roll.clientWidth)+",0,0,1,"+(/*-this.elem.clientWidth-*/-this.roll.clientWidth/2+w/2)+",0)";
               //$(this.roll).css("width",dx+"px"); 
               }
              else
               {
               //$(this.roll).css("width",(this.elem.clientWidth-dx)/2+"px"); 
                w = (this.elem.clientWidth-dx)/2;
                this.roll.style.transform="matrix("+(w/this.roll.clientWidth)+",0,0,1,"+(/*-this.elem.clientWidth-*/-this.roll.clientWidth/2+dx-w/2)+",0)";
               }

               // left = dx;

//              $(this.roll).css("right",(this.elem.clientWidth-dx)+"px"); 
          }

//          $(this.roll).css("box-shadow","5px 0px "+(Math.min(this.roll.clientWidth/3,50))+"px "+(Math.min(this.roll.clientWidth/6,25))+"px rgba(0,0,0,"+Math.min(this.roll.clientWidth/100,0.3)+")");
       }
     
    }


}

(function (){


  var root="";
  var updates = [];

  function MakeLiveThumb(el)
  {
   var src = el.dataset["src"];

  var docid = src.substr(src.lastIndexOf("/")+1);

  var div = document.createElement("DIV");
  var env = document.createElement("DIV");
  var type = "";
  if (el.dataset["type"]=="book"){
   type = "book";
  }
  env.className = "textagrafdoc";
  env.style.overflow='hidden';
  if (el.dataset["width"]) env.style.width = el.dataset["width"];
  if (el.dataset["height"]) env.style.height = el.dataset["height"];
  env.style.position = "relative";
  env.style.zIndex = 1;
  div.id = docid;
  div.style.position ="relative";
  div.style.margin ="auto";
  div.style.overflow = "hidden";
  div.dataset["n"] = 5;
  div.dataset["href"] = src;
  div.innerHTML = el.innerHTML;
  env.appendChild(div);
  var img = new Image();
  img.src=root+"api/docs/"+docid+"/thumb/1";
  img.style.maxWidth='100%';
  img.style.maxHeight='100%';

  img.dataset["src"] = root+"api/docs/"+docid+"/preview/1";

  div.appendChild(img);
  el.parentElement.replaceChild(env,el);
  var imgsrc = root+"api/docs/"+docid+"/body/";

  var update = function(){

     isVisible = function(img){
       var rect = img.getBoundingClientRect();
       return (rect.bottom>=0 && rect.top<=window.innerHeight && rect.left<=window.innerWidth && rect.right>=0)
    }

   if (isVisible(img)){

    if (type!="book"){

     

     new txtGLiveThumb(div,{
            loadfunc: function (self, image, n) {
                image.src = imgsrc + n + '.svg';
                  image.style.background = "white";
           }
     });
  
    }
    else
    {
      img.src = img.dataset["src"];
      img.style.position = "relative";
    }

     return false;
    }

   return true;
   }

   updates.push(update);

   img.onload = function(){ 
     this.onload = null;
     if (!update()) updates = updates.filter((i)=>i!=update);
    }
  }
 
  window.addEventListener('scroll',function(){
     for (var i=0;i<updates.length;i++)
      if (!updates[i]()) updates.splice(i,1);
   });

  document.addEventListener('DOMContentLoaded', function(){ 
   window.dispatchEvent(new Event('scroll'));});
  
 

  list = document.querySelectorAll("textagraf");
  for (i=0;i<list.length;i++)
  {
  var el = list[i];
  var src = el.dataset["src"];

  if (root=='')
  {
   root = src.substr(0,src.indexOf("/docs/")+1);
/*
   var styles = document.createElement('link');
   styles.rel = 'stylesheet';
   styles.type = 'text/css';
   styles.href = 'data:text/css;base64,'+btoa('.textagrafdoc img{position:absolute; max-width:100%;  max-height:100%;  background-color:#fff; background:url('+root+'images/loading.gif) center no-repeat; }');
   document.getElementsByTagName('head')[0].appendChild(styles);
*/
  }

  if (src.indexOf("/Groups/")>0){
    // docs list
     // load list and put it into div
     env = document.createElement("DIV");
     env.className = "textagraf-group";
     if (el.dataset["width"]) env.style.width = el.dataset["width"];
     if (el.dataset["height"]) env.style.height = el.dataset["height"];
     el.parentElement.replaceChild(env,el);

     var xhr = new XMLHttpRequest();
     xhr.open('GET', root+'api/Groups/Id/1', true);
     xhr.send();
     xhr.onreadystatechange = function() { // (3)
    if (xhr.readyState != 4) return;

     if (xhr.status == 200) {
       group = JSON.parse(xhr.responseText);
       group.Documents.forEach(function(doc){

            elem = document.createElement("div");
            env.appendChild(elem);
            elem.dataset["src"] = root+"docs/Item/"+doc.Id;
            elem.dataset["width"] = "300px";
            elem.dataset["height"] = "300px";
            elem.dataset["n"] = Math.max(10,doc.Pages);
            MakeLiveThumb(elem);

         });
     } 
    };
   }
 else
    MakeLiveThumb(el);

 }



})()
