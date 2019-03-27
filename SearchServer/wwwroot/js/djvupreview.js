
function imagedata_to_image(imagedata,type) {
    var canvas = document.createElement('canvas');
    var ctx = canvas.getContext('2d');
    canvas.width = imagedata.width;
    canvas.height = imagedata.height;
    ctx.putImageData(imagedata, 0, 0);

    var image = new Image();
    image.src = canvas.toDataURL(type);
    canvas = null;
    return image;
}


function create_foreground_image(data,mask){

        if (!mask) return (data);

        if (data && (data.data.length!=mask.data.length)){
          var canvas = document.createElement('canvas');
          canvas.width = mask.width;
          canvas.height = mask.height;
          var ctx = canvas.getContext('2d');
          ctx.putImageData(data, 0,0);
          ctx.scale(1.*mask.width/data.width,1.*mask.height/data.height);
          ctx.drawImage(canvas,0,0);
          data = ctx.getImageData(0,0,canvas.width,canvas.height);
          canvas = null;
        }

        var image = mask;

        for (var y=0;y<image.data.length;y+=4)
        {
          if (mask.data[y]!=0) // transparent i
           {
            image.data[y+3] = 0; 
           image.data[y+1] = 0; 
           image.data[y+2] = 0; 
           image.data[y+3] = 0; 
           }
          else
          if (data)
          { 
           image.data[y] = data.data[y];
           image.data[y+1] = data.data[y+1]; 
           image.data[y+2] = data.data[y+2]; 
           image.data[y+3] = data.data[y+3]; 
           
          }
      
       }
    return (image);
}

if (self.$){
(function($){
    $.fn.extend({
     loaddjvu: function (src){
      this.each(function(){loaddjvu(src,this);});
     }
   });}(jQuery)
);
}


function loaddjvu(src,el){

//        new Promise((resolve, reject) => {
                var xhr = new XMLHttpRequest();
                xhr.open("GET", src);
                xhr.responseType = 'arraybuffer';
                xhr.onload = (e) => {
                    if (xhr.status !== 200) {
                        return reject({ message: "Something went wrong!", xhr: xhr })
                    }
                    DjVu.IS_DEBUG && console.log("File loaded: ", e.loaded);
//                    resolve(xhr.response);

                    var r = xhr.response;
//            }).then((r)=>
{

// $(el).append($("<div>"+r.byteLength/1024+"</div>"));

 if (DjVu.IS_DEBUG) console.log('loaded page '+r.byteLength/1024.+' Kb');

 var startat = Date.now();
 var text="";

 var doc = new DjVu.Document(r);

 text+="doctime: "+(Date.now()-startat)+" ";
 startat = Date.now();
 
 if (doc.pages.length>0){
 
//  $(el).html("<div class='back'></div><div class='fore'></div>");

  var bdata = doc.pages[0].getBackgroundImageData();

// text+="getbkimage: "+(Date.now()-startat)+" ";
 startat = Date.now();

  if (bdata)
    {

     if (typeof (el)!='function')
     {

      var img = imagedata_to_image(bdata,'image/jpeg');
      $(img).addClass('back');
      $(el).append(img);

      text+="imagedata_to_image: "+(Date.now()-startat)+" ";
      startat = Date.now();

     }

   }

  var mask = doc.pages[0].getMaskImageData();

  text+="getMaskImageData: "+(Date.now()-startat)+" ";
  startat = Date.now();

  var data = doc.pages[0].getForegroundImageData();

  text+="getForegroundImageData: "+(Date.now()-startat)+" ";
  startat = Date.now();

  if (mask)
   {
     data = create_foreground_image(data,mask);
     text+="create_foreground_image: "+(Date.now()-startat)+" ";
     startat = Date.now();
    }

     if (typeof (el)!='function')
     {
  
       var img = imagedata_to_image(data,'image/png');
       $(img).addClass('fore');
       $(el).append(img);

       text+="imagedata_to_image png: "+(Date.now()-startat)+" ";
       startat = Date.now();
     }

   //$(el).append($("<div>"+text+"</div>"));
//   console.log(text);

   if (typeof (el)=='function'){
    el(bdata,data);
   }

   bdata = null;
   data = null;
   mask = null;
 }

 doc = null;
  
  }
 };
 xhr.send();


}

if (!self.document) {
     
      self.importScripts('djvu.js');

       //initWorker
      self.addEventListener('message',((e)=>{

           // data.src - source file
           // data.id - id of page
           loaddjvu (e.data.src,(b,d)=>{
             self.postMessage({'cmd':'done','page':e.data.id,'back':b,'fore':d});
            });


          }));
 }
else{

var djvuprevieworker;

function loaddjvuasync(src,el){

 djvuprevieworker.postMessage({'cmd':'loaddjvu','src':src,'id':el.id});

}


function initpreviewworker(path){

  djvuprevieworker = new Worker(path);

  djvuprevieworker.addEventListener('message', function(e) {
      if (e.data.cmd=='done'){
      var img;
      var el = document.getElementById(e.data.page);
      if (e.data.back){
       img  = imagedata_to_image(e.data.back,'image/jpeg');
       $(img).addClass('back');
       $(el).append(img);
      }

      if (e.data.fore){

       img = imagedata_to_image(e.data.fore,'image/png');
       $(img).addClass('fore');
       $(el).append(img);
                         
     }
    
    }
  }, false);
 }
}